package asn.diplomski.asn_app.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import asn.diplomski.asn_app.data.repository.AuthRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
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
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<AuthUiState>(AuthUiState.Idle)
    val uiState = _uiState.asStateFlow()

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
