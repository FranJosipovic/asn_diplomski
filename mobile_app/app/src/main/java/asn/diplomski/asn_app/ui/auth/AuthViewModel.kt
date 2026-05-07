package asn.diplomski.asn_app.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.repository.AuthRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface AuthUiState {
    data object Idle : AuthUiState
    data object Loading : AuthUiState
    data class Success(val tenantId: Long) : AuthUiState
    data class Error(val message: String) : AuthUiState
}

sealed interface AuthAction {
    data class SignIn(val email: String, val password: String) : AuthAction
}

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val tokenManager: TokenManager
) : ViewModel() {

    private val _uiState = MutableStateFlow<AuthUiState>(AuthUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val token = tokenManager.tokenFlow.firstOrNull()
            val tenantId = tokenManager.getTenantIdFromToken(token)
            if (tenantId == null) {
                _uiState.value = AuthUiState.Idle
                return@launch
            }
            // Refresh the access token so we don't land on devices with an expired token
            val refreshResult = authRepository.refreshToken()
            if (refreshResult.isFailure) {
                authRepository.logout()
                _uiState.value = AuthUiState.Idle
                return@launch
            }
            val freshToken = tokenManager.tokenFlow.firstOrNull()
            val freshTenantId = tokenManager.getTenantIdFromToken(freshToken) ?: tenantId
            _uiState.value = AuthUiState.Success(freshTenantId)
        }
    }

    fun onAction(action: AuthAction) {
        when (action) {
            is AuthAction.SignIn -> signIn(action.email, action.password)
        }
    }

    private fun signIn(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            val result = authRepository.signIn(email, password)
            result.onSuccess { tenantId ->
                _uiState.value = AuthUiState.Success(tenantId)
            }
            result.onFailure { e ->
                _uiState.value = AuthUiState.Error(e.message ?: "Unknown error")
            }
        }
    }
}
