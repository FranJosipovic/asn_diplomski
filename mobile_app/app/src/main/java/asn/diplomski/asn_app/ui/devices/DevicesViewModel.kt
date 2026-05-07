package asn.diplomski.asn_app.ui.devices

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.AuthRepository
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.Device
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import retrofit2.HttpException
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
    private val tokenManager: TokenManager,
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<DevicesUiState>(DevicesUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _navigateToAuth = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
    val navigateToAuth: SharedFlow<Unit> = _navigateToAuth

    fun onAction(action: DevicesAction) {
        when (action) {
            is DevicesAction.LoadDevices -> loadDevices()
        }
    }

    private fun loadDevices() {
        viewModelScope.launch {
            Log.d(TAG, "loadDevices: fetching /api/tenants/me")
            _uiState.value = DevicesUiState.Loading
            val token = tokenManager.tokenFlow.firstOrNull()
            val result = deviceRepository.getDevices(token)
            result.fold(
                onSuccess = { devices ->
                    Log.d(TAG, "loadDevices: success, ${devices.size} devices")
                    _uiState.value = if (devices.isEmpty()) DevicesUiState.Error("No devices found")
                                     else DevicesUiState.Success(devices)
                },
                onFailure = { e ->
                    if (e is HttpException && e.code() == 401) {
                        Log.w(TAG, "loadDevices: 401 — attempting token refresh")
                        handleUnauthorized()
                    } else {
                        Log.e(TAG, "loadDevices: failed", e)
                        _uiState.value = DevicesUiState.Error(e.message ?: "Unknown error")
                    }
                }
            )
        }
    }

    private suspend fun handleUnauthorized() {
        val refreshResult = authRepository.refreshToken()
        if (refreshResult.isSuccess) {
            Log.d(TAG, "handleUnauthorized: refresh succeeded, retrying")
            val newToken = tokenManager.tokenFlow.firstOrNull()
            val retryResult = deviceRepository.getDevices(newToken)
            retryResult.fold(
                onSuccess = { devices ->
                    _uiState.value = if (devices.isEmpty()) DevicesUiState.Error("No devices found")
                                     else DevicesUiState.Success(devices)
                },
                onFailure = { e ->
                    Log.e(TAG, "handleUnauthorized: retry failed", e)
                    authRepository.logout()
                    _navigateToAuth.tryEmit(Unit)
                }
            )
        } else {
            Log.w(TAG, "handleUnauthorized: refresh failed — navigating to auth")
            authRepository.logout()
            _navigateToAuth.tryEmit(Unit)
        }
    }
}
