package asn.diplomski.asn_app.ui.devices

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.Device
import asn.diplomski.asn_app.domain.model.ProvisionConfig
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import javax.inject.Inject

private const val TAG = "DevicesViewModel"

sealed interface DevicesUiState {
    data object Loading : DevicesUiState
    data class Success(val devices: List<Device>, val provisionConfig: ProvisionConfig?) : DevicesUiState
    data class Error(val message: String) : DevicesUiState
}

sealed interface DevicesAction {
    data class LoadDevices(val tenantId: Long) : DevicesAction
    data class StartProvisioning(val deviceId: Long) : DevicesAction
}

@HiltViewModel
class DevicesViewModel @Inject constructor(
    private val deviceRepository: DeviceRepository,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _uiState = MutableStateFlow<DevicesUiState>(DevicesUiState.Loading)
    val uiState = _uiState.asStateFlow()

    fun onAction(action: DevicesAction) {
        when (action) {
            is DevicesAction.LoadDevices -> loadDevices(action.tenantId)
            is DevicesAction.StartProvisioning -> startProvisioning(action.deviceId)
        }
    }

    private fun loadDevices(tenantId: Long) {
        viewModelScope.launch {
            Log.d(TAG, "loadDevices: tenantId=$tenantId")
            _uiState.value = DevicesUiState.Loading
            try {
                val token = tokenManager.tokenFlow.firstOrNull()
                Log.d(TAG, "loadDevices: token present=${token != null}")

                val devicesResult = deviceRepository.getDevices(tenantId, token)
                val configResult = deviceRepository.getProvisionConfig(token)

                val devices = devicesResult.getOrNull() ?: emptyList()
                val config = configResult.getOrNull()

                if (devicesResult.isFailure) {
                    Log.e(TAG, "loadDevices: getDevices failed", devicesResult.exceptionOrNull())
                }
                if (configResult.isFailure) {
                    Log.e(TAG, "loadDevices: getProvisionConfig failed", configResult.exceptionOrNull())
                }

                _uiState.value = if (devices.isEmpty()) {
                    Log.w(TAG, "loadDevices: no devices found for tenant $tenantId")
                    DevicesUiState.Error("No devices found")
                } else {
                    Log.d(TAG, "loadDevices: success, ${devices.size} devices, config present=${config != null}")
                    DevicesUiState.Success(devices, config)
                }
            } catch (e: Exception) {
                Log.e(TAG, "loadDevices: unexpected error", e)
                _uiState.value = DevicesUiState.Error(e.message ?: "Unknown error")
            }
        }
    }

    private fun startProvisioning(deviceId: Long) {
        Log.d(TAG, "startProvisioning: deviceId=$deviceId (not yet implemented)")
    }
}
