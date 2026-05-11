package asn.diplomski.asn_app.ui.devices

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.AuthRepository
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.Device
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import retrofit2.HttpException
import javax.inject.Inject

private const val TAG = "DevicesViewModel"
private const val POLL_INTERVAL_MS = 5_000L

sealed interface DevicesUiState {
    data object Loading : DevicesUiState
    data class Success(val devices: List<Device>) : DevicesUiState
    data class Error(val message: String) : DevicesUiState
}

sealed interface DevicesAction {
    data object Load : DevicesAction
    data object StartSystem : DevicesAction
    data object StopSystem : DevicesAction
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

    private val _systemCommandInProgress = MutableStateFlow(false)
    val systemCommandInProgress = _systemCommandInProgress.asStateFlow()

    private var pollingJob: Job? = null

    fun onAction(action: DevicesAction) {
        when (action) {
            DevicesAction.Load -> startPolling()
            DevicesAction.StartSystem -> sendSystemCommand(start = true)
            DevicesAction.StopSystem -> sendSystemCommand(start = false)
        }
    }

    private fun startPolling() {
        if (pollingJob?.isActive == true) return
        pollingJob = viewModelScope.launch {
            while (true) {
                val shouldContinue = fetchDevices()
                if (!shouldContinue) break
                delay(POLL_INTERVAL_MS)
            }
        }
    }

    override fun onCleared() {
        pollingJob?.cancel()
        super.onCleared()
    }

    private suspend fun fetchDevices(): Boolean {
        val token = tokenManager.tokenFlow.firstOrNull()
        val result = deviceRepository.getDevices(token)
        return result.fold(
            onSuccess = { devices ->
                Log.d(TAG, "fetchDevices: ${devices.size} devices")
                _uiState.value = if (devices.isEmpty()) DevicesUiState.Error("No devices found")
                                 else DevicesUiState.Success(devices)
                true
            },
            onFailure = { e ->
                if (e is HttpException && e.code() == 401) {
                    Log.w(TAG, "fetchDevices: 401 — attempting token refresh")
                    handleUnauthorized()
                    false
                } else {
                    Log.e(TAG, "fetchDevices: failed", e)
                    if (_uiState.value is DevicesUiState.Loading) {
                        _uiState.value = DevicesUiState.Error(e.message ?: "Unknown error")
                    }
                    true
                }
            }
        )
    }

    private suspend fun handleUnauthorized() {
        val refreshResult = authRepository.refreshToken()
        if (refreshResult.isSuccess) {
            Log.d(TAG, "handleUnauthorized: refresh succeeded, resuming poll")
            startPolling()
        } else {
            Log.w(TAG, "handleUnauthorized: refresh failed — navigating to auth")
            authRepository.logout()
            _navigateToAuth.tryEmit(Unit)
        }
    }

    private fun sendSystemCommand(start: Boolean) {
        viewModelScope.launch {
            _systemCommandInProgress.value = true
            val token = tokenManager.tokenFlow.firstOrNull()
            val result = if (start) deviceRepository.startProvision(token)
                         else deviceRepository.stopProvision(token)
            _systemCommandInProgress.value = false
            result.onFailure { e ->
                Log.e(TAG, "sendSystemCommand(start=$start): failed", e)
            }
        }
    }
}
