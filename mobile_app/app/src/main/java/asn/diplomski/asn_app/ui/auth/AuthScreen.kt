package asn.diplomski.asn_app.ui.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle

@Composable
internal fun AuthRoute(
    onNavigateToDevices: (tenantId: Long) -> Unit,
    viewModel: AuthViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    AuthScreen(
        uiState = uiState,
        onAction = viewModel::onAction,
        onNavigateToDevices = onNavigateToDevices
    )
}

@Composable
internal fun AuthScreen(
    uiState: AuthUiState,
    onAction: (AuthAction) -> Unit,
    onNavigateToDevices: (tenantId: Long) -> Unit
) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }

    when (uiState) {
        is AuthUiState.Success -> {
            onNavigateToDevices(uiState.tenantId)
        }
        else -> {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = "Irrigation System",
                    style = MaterialTheme.typography.headlineLarge
                )

                Spacer(modifier = Modifier.height(32.dp))

                TextField(
                    value = email,
                    onValueChange = { email = it },
                    label = { Text("Email") },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = uiState !is AuthUiState.Loading
                )

                Spacer(modifier = Modifier.height(16.dp))

                TextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Password") },
                    modifier = Modifier.fillMaxWidth(),
                    visualTransformation = PasswordVisualTransformation(),
                    enabled = uiState !is AuthUiState.Loading
                )

                Spacer(modifier = Modifier.height(32.dp))

                when (uiState) {
                    is AuthUiState.Idle -> {
                        Button(
                            onClick = {
                                onAction(AuthAction.SignIn(email, password))
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Sign In")
                        }
                    }
                    is AuthUiState.Loading -> {
                        CircularProgressIndicator()
                    }
                    is AuthUiState.Error -> {
                        Text(
                            text = uiState.message,
                            color = MaterialTheme.colorScheme.error,
                            modifier = Modifier.padding(bottom = 16.dp)
                        )
                        Button(
                            onClick = {
                                onAction(AuthAction.SignIn(email, password))
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Try Again")
                        }
                    }
                    is AuthUiState.Success -> {}
                }
            }
        }
    }
}
