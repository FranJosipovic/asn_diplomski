package asn.diplomski.asn_app.data.api.models

data class ProvisioningTokenResponse(
    val expiresAt: String,
    val serverHost: String,
    val serverPort: Int,
    val devices: List<DeviceProvisioningDto>
)

data class DeviceProvisioningDto(
    val deviceId: Long,
    val deviceType: String,
    val deviceSsid: String,
    val provisionStatus: String,
    val provisioningToken: String,
    val sensors: List<SensorProvisioningDto>
)

data class SensorProvisioningDto(
    val sensorId: Long,
    val sensorType: String
)
