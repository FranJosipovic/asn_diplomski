package asn.diplomski.asn_app.ui.devices

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import kotlinx.coroutines.flow.collectLatest
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import asn.diplomski.asn_app.domain.model.Device

private val COMMANDABLE_STATUSES = setOf("Ready", "Working", "Stopped")

@Composable
internal fun DevicesRoute(
    onNavigateToAuth: () -> Unit,
    viewModel: DevicesViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val systemCommandInProgress by viewModel.systemCommandInProgress.collectAsState()

    LaunchedEffect(viewModel) {
        viewModel.navigateToAuth.collectLatest { onNavigateToAuth() }
    }

    LaunchedEffect(Unit) {
        viewModel.onAction(DevicesAction.Load)
    }

    DevicesScreen(
        uiState = uiState,
        systemCommandInProgress = systemCommandInProgress,
        onStartSystem = { viewModel.onAction(DevicesAction.StartSystem) },
        onStopSystem = { viewModel.onAction(DevicesAction.StopSystem) }
    )
}

@Composable
internal fun DevicesScreen(
    uiState: DevicesUiState,
    systemCommandInProgress: Boolean = false,
    onStartSystem: () -> Unit = {},
    onStopSystem: () -> Unit = {}
) {
    Box(modifier = Modifier.fillMaxSize()) {
        when (uiState) {
            is DevicesUiState.Loading -> {
                CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
            }
            is DevicesUiState.Success -> {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(16.dp)
                ) {
                    Text(text = "Devices", style = MaterialTheme.typography.headlineLarge)
                    Spacer(modifier = Modifier.height(12.dp))

                    val hasCommandable = uiState.devices.any {
                        it.provisionStatus in COMMANDABLE_STATUSES
                    }

                    if (hasCommandable) {
                        if (systemCommandInProgress) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                CircularProgressIndicator(modifier = Modifier.size(20.dp))
                                Text(
                                    text = "Sending command…",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                        } else {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                Button(
                                    onClick = onStartSystem,
                                    modifier = Modifier.weight(1f)
                                ) {
                                    Text("Start System")
                                }
                                OutlinedButton(
                                    onClick = onStopSystem,
                                    modifier = Modifier.weight(1f)
                                ) {
                                    Text("Stop System")
                                }
                            }
                        }
                        Spacer(modifier = Modifier.height(12.dp))
                    }

                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(uiState.devices) { device ->
                            DeviceCard(device = device)
                        }
                    }
                }
            }
            is DevicesUiState.Error -> {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(16.dp),
                    verticalArrangement = Arrangement.Center,
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(text = uiState.message, color = MaterialTheme.colorScheme.error)
                }
            }
        }
    }
}

@Composable
private fun DeviceCard(device: Device) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = device.description ?: "Device ${device.deviceNumber}",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = device.type,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                StatusBadge(status = device.provisionStatus ?: "Unknown")
            }

            if (device.sensors.isNotEmpty()) {
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = "Sensors",
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.Bold
                )
                device.sensors.forEach { sensor ->
                    Text(
                        text = "• ${sensor.description ?: "Sensor ${sensor.sensorNumber}"} (${sensor.type})",
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(start = 8.dp, top = 4.dp)
                    )
                }
            }
        }
    }
}

@Composable
private fun StatusBadge(status: String) {
    val color = when (status) {
        "Working"            -> MaterialTheme.colorScheme.primary
        "Ready"              -> MaterialTheme.colorScheme.tertiary
        "Stopped"            -> MaterialTheme.colorScheme.error
        "Provisioning",
        "ProvisioningReady"  -> MaterialTheme.colorScheme.secondary
        else                 -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Text(
        text = status,
        style = MaterialTheme.typography.labelSmall,
        color = color,
        modifier = Modifier
            .background(color.copy(alpha = 0.12f), shape = RoundedCornerShape(4.dp))
            .padding(horizontal = 8.dp, vertical = 3.dp)
    )
}
