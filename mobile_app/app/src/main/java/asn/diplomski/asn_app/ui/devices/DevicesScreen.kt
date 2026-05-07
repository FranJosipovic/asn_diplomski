package asn.diplomski.asn_app.ui.devices

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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
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

@Composable
internal fun DevicesRoute(
    tenantId: Long,
    onNavigateToProvisioning: (deviceId: Long) -> Unit,
    onNavigateToAuth: () -> Unit,
    viewModel: DevicesViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    LaunchedEffect(viewModel) {
        viewModel.navigateToAuth.collectLatest { onNavigateToAuth() }
    }

    LaunchedEffect(tenantId) {
        viewModel.onAction(DevicesAction.LoadDevices(tenantId))
    }

    DevicesScreen(
        uiState = uiState,
        onProvision = onNavigateToProvisioning
    )
}

@Composable
internal fun DevicesScreen(
    uiState: DevicesUiState,
    onProvision: (deviceId: Long) -> Unit
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
                    Spacer(modifier = Modifier.height(16.dp))
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        items(uiState.devices) { device ->
                            DeviceCard(
                                device = device,
                                onProvision = { onProvision(device.id) }
                            )
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
private fun DeviceCard(device: Device, onProvision: () -> Unit) {
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
                        text = "Type: ${device.type}",
                        style = MaterialTheme.typography.bodySmall
                    )
                    Text(
                        text = "Status: ${device.provisionStatus ?: "Unknown"}",
                        style = MaterialTheme.typography.bodySmall,
                        color = when (device.provisionStatus) {
                            "Provisioned"    -> MaterialTheme.colorScheme.primary
                            "Provisioning"   -> MaterialTheme.colorScheme.tertiary
                            else             -> MaterialTheme.colorScheme.onSurfaceVariant
                        }
                    )
                }

                if (device.provisionStatus == "Provisioned") {
                    Icon(
                        imageVector = Icons.Default.CheckCircle,
                        contentDescription = "Provisioned",
                        modifier = Modifier.size(28.dp),
                        tint = MaterialTheme.colorScheme.primary
                    )
                } else {
                    Button(onClick = onProvision) {
                        Text("Provision")
                    }
                }
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
                        text = "• ${sensor.description ?: "Sensor ${sensor.sensorNumber}"} (Type: ${sensor.type})",
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(start = 8.dp, top = 4.dp)
                    )
                }
            }
        }
    }
}
