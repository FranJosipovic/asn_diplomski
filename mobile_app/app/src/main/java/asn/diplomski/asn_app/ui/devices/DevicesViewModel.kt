package asn.diplomski.asn_app.ui.devices

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.Device
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import javax.inject.Inject

private const val TAG = "DevicesViewModel"

sealed interface DevicesUiState {
    data object Loading : DevicesUiState
    data class Success(val devices: List<Device>) : DevicesUiState
    data class Error(val message: String) : DevicesUiState
}

sealed interface DevicesAction {
    data class LoadDevices(val tenantId: Long) : DevicesAction
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
            is DevicesAction.LoadDevices -> loadDevices()
        }
    }

    private fun loadDevices() {
        viewModelScope.launch {
            Log.d(TAG, "loadDevices: fetching /api/tenants/me")
            _uiState.value = DevicesUiState.Loading
            try {
                val token = tokenManager.tokenFlow.firstOrNull()
                val result = deviceRepository.getDevices(token)
                _uiState.value = result.fold(
                    onSuccess = { devices ->
                        Log.d(TAG, "loadDevices: success, ${devices.size} devices")
                        if (devices.isEmpty()) DevicesUiState.Error("No devices found")
                        else DevicesUiState.Success(devices)
                    },
                    onFailure = { e ->
                        Log.e(TAG, "loadDevices: failed", e)
                        DevicesUiState.Error(e.message ?: "Unknown error")
                    }
                )
            } catch (e: Exception) {
                Log.e(TAG, "loadDevices: unexpected error", e)
                _uiState.value = DevicesUiState.Error(e.message ?: "Unknown error")
            }
        }
    }
}
