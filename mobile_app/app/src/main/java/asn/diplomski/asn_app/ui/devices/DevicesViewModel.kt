package asn.diplomski.asn_app.ui.devices

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.Device
import asn.diplomski.asn_app.domain.model.ProvisionConfig
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

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

    private val _currentTenantId = MutableStateFlow<Long?>(null)

    init {
        viewModelScope.launch {
            tokenManager.tokenFlow.collectLatest { token ->
                _currentTenantId.value = tokenManager.getTenantIdFromToken(token)
            }
        }
    }

    fun onAction(action: DevicesAction) {
        when (action) {
            is DevicesAction.LoadDevices -> loadDevices(action.tenantId)
            is DevicesAction.StartProvisioning -> startProvisioning(action.deviceId)
        }
    }

    private fun loadDevices(tenantId: Long) {
        _currentTenantId.value = tenantId
        viewModelScope.launch {
            try {
                tokenManager.tokenFlow.collectLatest { token ->
                    combine(
                        deviceRepository.getDevices(tenantId, token),
                        deviceRepository.getProvisionConfig(token)
                    ) { devicesResult, configResult ->
                        val devices = devicesResult.getOrNull() ?: emptyList()
                        val config = configResult.getOrNull()

                        if (devices.isEmpty()) {
                            _uiState.value = DevicesUiState.Error("No devices found")
                        } else {
                            _uiState.value = DevicesUiState.Success(devices, config)
                        }
                    }.collect {}
                }
            } catch (e: Exception) {
                _uiState.value = DevicesUiState.Error(e.message ?: "Unknown error")
            }
        }
    }

    private fun startProvisioning(deviceId: Long) {
        // Implementation will be added later when provisioning feature is ready
    }
}
