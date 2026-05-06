package asn.diplomski.asn_app.ui.provisioning

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.FormBody
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.IOException
import javax.inject.Inject

private const val TAG = "ProvisioningViewModel"
private const val WIFI_MANAGER_URL = "http://192.168.4.1/wifisave"

sealed interface ProvisioningUiState {
    data object Idle : ProvisioningUiState
    data object Provisioning : ProvisioningUiState
    data object Success : ProvisioningUiState
    data class Error(val message: String) : ProvisioningUiState
}

sealed interface ProvisioningAction {
    data class Provision(val wifiSsid: String, val wifiPassword: String) : ProvisioningAction
    data object Retry : ProvisioningAction
}

@HiltViewModel
class ProvisioningViewModel @Inject constructor(
    private val okHttpClient: OkHttpClient
) : ViewModel() {

    private val _uiState = MutableStateFlow<ProvisioningUiState>(ProvisioningUiState.Idle)
    val uiState = _uiState.asStateFlow()

    fun onAction(action: ProvisioningAction) {
        when (action) {
            is ProvisioningAction.Provision -> provision(action.wifiSsid, action.wifiPassword)
            ProvisioningAction.Retry -> _uiState.value = ProvisioningUiState.Idle
        }
    }

    private fun provision(ssid: String, password: String) {
        Log.d(TAG, "provision: sending credentials for ssid=$ssid to $WIFI_MANAGER_URL")
        _uiState.value = ProvisioningUiState.Provisioning
        viewModelScope.launch {
            try {
                val body = FormBody.Builder()
                    .add("s", ssid)
                    .add("p", password)
                    .build()
                val request = Request.Builder()
                    .url(WIFI_MANAGER_URL)
                    .post(body)
                    .build()
                val response = withContext(Dispatchers.IO) {
                    okHttpClient.newCall(request).execute()
                }
                Log.d(TAG, "provision: response code=${response.code}")
                response.close()
                _uiState.value = ProvisioningUiState.Success
            } catch (e: IOException) {
                Log.e(TAG, "provision: network error", e)
                _uiState.value = ProvisioningUiState.Error(
                    "Could not reach the device.\n\nMake sure your phone is connected to the \"Irrigation-Setup\" WiFi network."
                )
            }
        }
    }
}
