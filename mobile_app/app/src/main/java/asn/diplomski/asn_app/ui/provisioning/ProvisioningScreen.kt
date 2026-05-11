package asn.diplomski.asn_app.ui.provisioning

import androidx.compose.foundation.clickable
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
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Done
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import asn.diplomski.asn_app.domain.model.ProvisionDevice
import com.espressif.provisioning.WiFiAccessPoint

@Composable
internal fun ProvisioningRoute(
    viewModel: ProvisioningViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.onAction(ProvisioningAction.LoadDevices)
    }

    ProvisioningScreen(
        uiState = uiState,
        onAction = viewModel::onAction
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
internal fun ProvisioningScreen(
    uiState: ProvisioningUiState,
    onAction: (ProvisioningAction) -> Unit
) {
    val inProvisioningFlow = uiState !is ProvisioningUiState.LoadingDevices &&
            uiState !is ProvisioningUiState.DeviceList

    Scaffold(
        topBar = {
            if (inProvisioningFlow) {
                TopAppBar(
                    title = { Text("Provision Device") },
                    navigationIcon = {
                        IconButton(onClick = { onAction(ProvisioningAction.BackToList) }) {
                            Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                        }
                    }
                )
            } else {
                TopAppBar(title = { Text("Provisioning") })
            }
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp)
        ) {
            when (uiState) {
                is ProvisioningUiState.LoadingDevices -> LoadingStep("Loading devices…")
                is ProvisioningUiState.DeviceList -> DeviceListStep(uiState.devices, onAction)
                is ProvisioningUiState.Idle -> IdleStep(onAction)
                is ProvisioningUiState.FetchingConfig -> LoadingStep("Fetching provisioning config…")
                is ProvisioningUiState.ConnectingToDevice -> LoadingStep("Connecting to device…")
                is ProvisioningUiState.SendingProvisionData -> LoadingStep("Sending provisioning data…")
                is ProvisioningUiState.ScanningNetworks -> LoadingStep("Scanning for WiFi networks…")
                is ProvisioningUiState.NetworksFound -> SelectNetworkStep(uiState.networks, onAction)
                is ProvisioningUiState.Provisioning -> LoadingStep("Provisioning device…")
                is ProvisioningUiState.WaitingForMqtt -> LoadingStep("Waiting for device to connect to MQTT…")
                is ProvisioningUiState.Success -> SuccessStep(onAction)
                is ProvisioningUiState.Error -> ErrorStep(uiState.message, onAction)
            }
        }
    }
}

@Composable
private fun DeviceListStep(
    devices: List<ProvisionDevice>,
    onAction: (ProvisioningAction) -> Unit
) {
    if (devices.isEmpty()) {
        Column(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "No devices found.",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.height(16.dp))
            OutlinedButton(onClick = { onAction(ProvisioningAction.LoadDevices) }) {
                Text("Refresh")
            }
        }
        return
    }

    Column {
        Text(
            text = "Select a device to provision",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(12.dp))
        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            items(devices) { device ->
                ProvisionDeviceCard(device = device, onAction = onAction)
            }
        }
    }
}

@Composable
private fun ProvisionDeviceCard(
    device: ProvisionDevice,
    onAction: (ProvisioningAction) -> Unit
) {
    val canProvision = device.provisionStatus == "NotProvisioned" ||
            device.provisionStatus == "ProvisioningReady"

    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = device.deviceSsid,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = device.deviceType,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = device.provisionStatus,
                    style = MaterialTheme.typography.bodySmall,
                    color = when (device.provisionStatus) {
                        "Ready", "Working" -> MaterialTheme.colorScheme.primary
                        "Provisioning", "ProvisioningReady" -> MaterialTheme.colorScheme.tertiary
                        else -> MaterialTheme.colorScheme.onSurfaceVariant
                    }
                )
            }
            if (canProvision) {
                Button(onClick = { onAction(ProvisioningAction.SelectDevice(device.deviceId)) }) {
                    Text("Provision")
                }
            }
        }
    }
}

@Composable
private fun IdleStep(onAction: (ProvisioningAction) -> Unit) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        Text(text = "Ready to Provision", style = MaterialTheme.typography.titleLarge)
        Text(
            text = "Make sure the device is powered on and in provisioning mode (blinking purple LED).",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(8.dp))
        Button(
            onClick = { onAction(ProvisioningAction.StartProvisioning) },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Start Provisioning")
        }
    }
}

@Composable
private fun SelectNetworkStep(
    networks: List<WiFiAccessPoint>,
    onAction: (ProvisioningAction) -> Unit
) {
    var selectedSsid by remember { mutableStateOf("") }
    var wifiPassword by remember { mutableStateOf("") }

    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(text = "Select Home WiFi", style = MaterialTheme.typography.titleLarge)
        Text(
            text = "Choose the WiFi network the device should connect to.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(4.dp))

        LazyColumn(
            modifier = Modifier.weight(1f),
            verticalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            items(networks) { ap ->
                NetworkItem(
                    ssid = ap.wifiName,
                    isSelected = selectedSsid == ap.wifiName,
                    onClick = { selectedSsid = ap.wifiName }
                )
            }
        }

        if (selectedSsid.isNotBlank()) {
            OutlinedTextField(
                value = wifiPassword,
                onValueChange = { wifiPassword = it },
                label = { Text("WiFi Password") },
                modifier = Modifier.fillMaxWidth(),
                visualTransformation = PasswordVisualTransformation(),
                singleLine = true
            )
        }

        Button(
            onClick = { onAction(ProvisioningAction.ProvisionWithNetwork(selectedSsid, wifiPassword)) },
            modifier = Modifier.fillMaxWidth(),
            enabled = selectedSsid.isNotBlank()
        ) {
            Text("Provision Device")
        }
    }
}

@Composable
private fun NetworkItem(ssid: String, isSelected: Boolean, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        colors = CardDefaults.cardColors(
            containerColor = if (isSelected) MaterialTheme.colorScheme.primaryContainer
            else MaterialTheme.colorScheme.surface
        )
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(text = ssid, modifier = Modifier.weight(1f))
            RadioButton(selected = isSelected, onClick = onClick)
        }
    }
}

@Composable
private fun LoadingStep(message: String) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        CircularProgressIndicator()
        Spacer(modifier = Modifier.height(16.dp))
        Text(text = message, style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun SuccessStep(onAction: (ProvisioningAction) -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(
            imageVector = Icons.Default.Done,
            contentDescription = null,
            modifier = Modifier.size(72.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(modifier = Modifier.height(16.dp))
        Text(text = "Device Provisioned!", style = MaterialTheme.typography.headlineSmall)
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = "The device connected to WiFi and confirmed its MQTT connection.",
            style = MaterialTheme.typography.bodyMedium,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(32.dp))
        Button(
            onClick = { onAction(ProvisioningAction.BackToList) },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Back to Device List")
        }
    }
}

@Composable
private fun ErrorStep(message: String, onAction: (ProvisioningAction) -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "Error",
            style = MaterialTheme.typography.headlineSmall,
            color = MaterialTheme.colorScheme.error
        )
        Spacer(modifier = Modifier.height(12.dp))
        Text(
            text = message,
            style = MaterialTheme.typography.bodyMedium,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(32.dp))
        Button(
            onClick = { onAction(ProvisioningAction.Retry) },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Try Again")
        }
        Spacer(modifier = Modifier.height(8.dp))
        OutlinedButton(
            onClick = { onAction(ProvisioningAction.BackToList) },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Back to Device List")
        }
    }
}
