package asn.diplomski.asn_app.ui.provisioning

import android.content.Context
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.DeviceProvisionInfo
import com.espressif.provisioning.DeviceConnectionEvent
import com.espressif.provisioning.ESPConstants
import com.espressif.provisioning.ESPDevice
import com.espressif.provisioning.ESPProvisionManager
import com.espressif.provisioning.WiFiAccessPoint
import com.espressif.provisioning.listeners.ProvisionListener
import com.espressif.provisioning.listeners.WiFiScanListener
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.greenrobot.eventbus.EventBus
import org.greenrobot.eventbus.Subscribe
import org.greenrobot.eventbus.ThreadMode
import org.json.JSONObject
import javax.inject.Inject

private const val TAG = "ProvisioningViewModel"

sealed interface ProvisioningUiState {
    data object Idle : ProvisioningUiState
    data object FetchingConfig : ProvisioningUiState
    data object ConnectingToDevice : ProvisioningUiState
    data object SendingProvisionData : ProvisioningUiState
    data object ScanningNetworks : ProvisioningUiState
    data class NetworksFound(val networks: List<WiFiAccessPoint>) : ProvisioningUiState
    data object Provisioning : ProvisioningUiState
    data object WaitingForMqtt : ProvisioningUiState
    data object Success : ProvisioningUiState
    data class Error(val message: String) : ProvisioningUiState
}

sealed interface ProvisioningAction {
    data object StartProvisioning : ProvisioningAction
    data class ProvisionWithNetwork(val wifiSsid: String, val wifiPassword: String) : ProvisioningAction
    data object Retry : ProvisioningAction
}

@HiltViewModel
class ProvisioningViewModel @Inject constructor(
    @ApplicationContext private val context: Context,
    private val deviceRepository: DeviceRepository,
    private val tokenManager: TokenManager,
    private val okHttpClient: OkHttpClient
) : ViewModel() {

    private val _uiState = MutableStateFlow<ProvisioningUiState>(ProvisioningUiState.Idle)
    val uiState = _uiState.asStateFlow()

    private var espDevice: ESPDevice? = null
    private var provisionInfo: DeviceProvisionInfo? = null
    private var deviceId: Long = -1L
    private var pollingJob: Job? = null

    init {
        EventBus.getDefault().register(this)
    }

    override fun onCleared() {
        EventBus.getDefault().unregister(this)
        espDevice?.disconnectDevice()
        pollingJob?.cancel()
        super.onCleared()
    }

    fun setDeviceId(id: Long) {
        deviceId = id
    }

    fun onAction(action: ProvisioningAction) {
        when (action) {
            is ProvisioningAction.StartProvisioning -> fetchConfigAndConnect()
            is ProvisioningAction.ProvisionWithNetwork -> provision(action.wifiSsid, action.wifiPassword)
            ProvisioningAction.Retry -> {
                pollingJob?.cancel()
                pollingJob = null
                espDevice?.disconnectDevice()
                espDevice = null
                provisionInfo = null
                _uiState.value = ProvisioningUiState.Idle
            }
        }
    }

    private fun fetchConfigAndConnect() {
        viewModelScope.launch {
            Log.d(TAG, "fetchConfigAndConnect: deviceId=$deviceId")
            _uiState.value = ProvisioningUiState.FetchingConfig
            val token = tokenManager.tokenFlow.firstOrNull()
            val result = deviceRepository.getDeviceProvisionInfo(deviceId, token)
            result.fold(
                onSuccess = { info ->
                    Log.d(TAG, "fetchConfigAndConnect: got config, ssid=${info.deviceSsid}")
                    provisionInfo = info
                    connectToDevice(info.deviceSsid)
                },
                onFailure = { e ->
                    Log.e(TAG, "fetchConfigAndConnect: failed", e)
                    _uiState.value = ProvisioningUiState.Error("Failed to get provisioning config: ${e.message}")
                }
            )
        }
    }

    private fun connectToDevice(ssid: String) {
        Log.d(TAG, "connectToDevice: ssid=$ssid")
        _uiState.value = ProvisioningUiState.ConnectingToDevice
        espDevice = ESPProvisionManager.getInstance(context)
            .createESPDevice(ESPConstants.TransportType.TRANSPORT_SOFTAP, ESPConstants.SecurityType.SECURITY_0)
        espDevice?.connectWiFiDevice(ssid, "")
    }

    @Subscribe(threadMode = ThreadMode.MAIN)
    fun onDeviceConnectionEvent(event: DeviceConnectionEvent) {
        Log.d(TAG, "onDeviceConnectionEvent: type=${event.eventType}")
        when (event.eventType) {
            ESPConstants.EVENT_DEVICE_CONNECTED -> {
                Log.d(TAG, "Device connected — sending provisioning data")
                _uiState.value = ProvisioningUiState.SendingProvisionData
                sendProvisionData()
            }
            ESPConstants.EVENT_DEVICE_CONNECTION_FAILED -> {
                Log.e(TAG, "Device connection failed")
                _uiState.value = ProvisioningUiState.Error(
                    "Could not connect to the device.\nMake sure it is powered on and in provisioning mode."
                )
            }
            ESPConstants.EVENT_DEVICE_DISCONNECTED -> {
                if (_uiState.value !is ProvisioningUiState.Success) {
                    Log.w(TAG, "Device disconnected unexpectedly")
                    _uiState.value = ProvisioningUiState.Error("Device disconnected unexpectedly.")
                }
            }
        }
    }

    private fun sendProvisionData() {
        val info = provisionInfo ?: run {
            _uiState.value = ProvisioningUiState.Error("Provisioning config missing.")
            return
        }
        val json = JSONObject().apply {
            put("token", info.provisioningToken)
            put("host", info.serverHost)
            put("port", info.serverPort)
        }.toString()

        Log.d(TAG, "sendProvisionData: POST http://192.168.4.1/prov-data")
        viewModelScope.launch {
            val connectivityManager = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager

            // Find the active WiFi network and bind to it so Android doesn't
            // reroute the request through cellular when the ESP32 AP has no internet
            val wifiNetwork = connectivityManager.allNetworks.firstOrNull { network ->
                connectivityManager.getNetworkCapabilities(network)
                    ?.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) == true
            }

            val client = if (wifiNetwork != null) {
                Log.d(TAG, "sendProvisionData: binding socket to WiFi network")
                okHttpClient.newBuilder()
                    .socketFactory(wifiNetwork.socketFactory)
                    .build()
            } else {
                Log.w(TAG, "sendProvisionData: WiFi network not found, using default client")
                okHttpClient
            }

            try {
                val body = json.toRequestBody("application/json".toMediaType())
                val request = Request.Builder()
                    .url("http://192.168.4.1/prov-data")
                    .post(body)
                    .build()
                withContext(Dispatchers.IO) {
                    client.newCall(request).execute().close()
                }
                Log.d(TAG, "sendProvisionData: success")
                _uiState.value = ProvisioningUiState.ScanningNetworks
                scanNetworks()
            } catch (e: Exception) {
                Log.e(TAG, "sendProvisionData: failed", e)
                _uiState.value = ProvisioningUiState.Error("Failed to send data to device: ${e.message}")
            }
        }
    }

    private fun scanNetworks() {
        espDevice?.scanNetworks(object : WiFiScanListener {
            override fun onWifiListReceived(wifiList: ArrayList<WiFiAccessPoint>) {
                Log.d(TAG, "scanNetworks: found ${wifiList.size} networks")
                _uiState.value = ProvisioningUiState.NetworksFound(wifiList)
            }

            override fun onWiFiScanFailed(e: Exception) {
                Log.e(TAG, "scanNetworks: failed", e)
                _uiState.value = ProvisioningUiState.Error("Network scan failed: ${e.message}")
            }
        }) ?: run {
            _uiState.value = ProvisioningUiState.Error("Device not connected.")
        }
    }

    private fun provision(ssid: String, password: String) {
        Log.d(TAG, "provision: ssid=$ssid")
        _uiState.value = ProvisioningUiState.Provisioning
        espDevice?.provision(ssid, password, object : ProvisionListener {
            override fun createSessionFailed(e: Exception) {
                Log.e(TAG, "createSessionFailed", e)
                _uiState.value = ProvisioningUiState.Error("Session creation failed: ${e.message}")
            }

            override fun wifiConfigSent() { Log.d(TAG, "wifiConfigSent") }

            override fun wifiConfigFailed(e: Exception) {
                Log.e(TAG, "wifiConfigFailed", e)
                _uiState.value = ProvisioningUiState.Error("Failed to send WiFi config: ${e.message}")
            }

            override fun wifiConfigApplied() { Log.d(TAG, "wifiConfigApplied") }

            override fun wifiConfigApplyFailed(e: Exception) {
                Log.e(TAG, "wifiConfigApplyFailed", e)
                _uiState.value = ProvisioningUiState.Error("Failed to apply WiFi config: ${e.message}")
            }

            override fun provisioningFailedFromDevice(failureReason: ESPConstants.ProvisionFailureReason) {
                Log.e(TAG, "provisioningFailedFromDevice: $failureReason")
                val msg = when (failureReason) {
                    ESPConstants.ProvisionFailureReason.AUTH_FAILED -> "Authentication failed. Check the WiFi password."
                    ESPConstants.ProvisionFailureReason.NETWORK_NOT_FOUND -> "WiFi network not found."
                    ESPConstants.ProvisionFailureReason.DEVICE_DISCONNECTED -> "Device disconnected during provisioning."
                    else -> "Provisioning failed on device: $failureReason"
                }
                _uiState.value = ProvisioningUiState.Error(msg)
            }

            override fun deviceProvisioningSuccess() {
                Log.d(TAG, "deviceProvisioningSuccess — starting MQTT poll")
                _uiState.value = ProvisioningUiState.WaitingForMqtt
                startMqttPolling()
            }

            override fun onProvisioningFailed(e: Exception) {
                Log.e(TAG, "onProvisioningFailed", e)
                _uiState.value = ProvisioningUiState.Error("Provisioning failed: ${e.message}")
            }
        }) ?: run {
            _uiState.value = ProvisioningUiState.Error("Device not connected.")
        }
    }

    private fun startMqttPolling() {
        pollingJob?.cancel()
        pollingJob = viewModelScope.launch {
            val token = tokenManager.tokenFlow.firstOrNull()
            val deadline = System.currentTimeMillis() + 2 * 60 * 1000L
            delay(5_000)
            while (System.currentTimeMillis() < deadline) {
                val result = deviceRepository.getDeviceProvisionStatus(deviceId, token)
                result.onSuccess { status ->
                    Log.d(TAG, "poll: deviceId=$deviceId status=$status")
                    if (status == "Provisioned") {
                        _uiState.value = ProvisioningUiState.Success
                        return@launch
                    }
                }
                delay(5_000)
            }
            if (_uiState.value is ProvisioningUiState.WaitingForMqtt) {
                Log.w(TAG, "poll: timed out waiting for MQTT confirmation")
                _uiState.value = ProvisioningUiState.Error(
                    "Device did not confirm MQTT connection within 2 minutes.\nCheck that the MQTT broker is reachable."
                )
            }
        }
    }
}
