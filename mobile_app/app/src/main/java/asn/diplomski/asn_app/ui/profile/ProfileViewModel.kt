package asn.diplomski.asn_app.ui.profile

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.AuthRepository
import asn.diplomski.asn_app.data.repository.DeviceRepository
import asn.diplomski.asn_app.domain.model.TenantProfile
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import javax.inject.Inject

private const val TAG = "ProfileViewModel"

sealed interface ProfileUiState {
    data object Loading : ProfileUiState
    data class Success(val profile: TenantProfile) : ProfileUiState
    data class Error(val message: String) : ProfileUiState
}

sealed interface ProfileAction {
    data object Load : ProfileAction
    data object Logout : ProfileAction
}

@HiltViewModel
class ProfileViewModel @Inject constructor(
    private val deviceRepository: DeviceRepository,
    private val tokenManager: TokenManager,
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<ProfileUiState>(ProfileUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _navigateToAuth = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
    val navigateToAuth: SharedFlow<Unit> = _navigateToAuth

    fun onAction(action: ProfileAction) {
        when (action) {
            ProfileAction.Load -> loadProfile()
            ProfileAction.Logout -> logout()
        }
    }

    private fun loadProfile() {
        viewModelScope.launch {
            _uiState.value = ProfileUiState.Loading
            val token = tokenManager.tokenFlow.firstOrNull()
            val result = deviceRepository.getTenantProfile(token)
            result.fold(
                onSuccess = { profile ->
                    Log.d(TAG, "loadProfile: loaded for ${profile.email}")
                    _uiState.value = ProfileUiState.Success(profile)
                },
                onFailure = { e ->
                    Log.e(TAG, "loadProfile: failed", e)
                    _uiState.value = ProfileUiState.Error(e.message ?: "Failed to load profile")
                }
            )
        }
    }

    private fun logout() {
        viewModelScope.launch {
            tokenManager.clearTokens()
            _navigateToAuth.tryEmit(Unit)
        }
    }
}
