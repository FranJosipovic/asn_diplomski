package asn.diplomski.asn_app.domain.model

data class Device(
    val id: Long,
    val type: Int,
    val provisionStatus: String?,
    val deviceNumber: Int,
    val description: String?,
    val isActive: Boolean,
    val createdAt: String,
    val lastSeenAt: String?,
    val sensors: List<Sensor>
)

data class Sensor(
    val id: Long,
    val type: Int,
    val sensorNumber: Int,
    val description: String?,
    val isActive: Boolean,
    val createdAt: String
)

data class DeviceProvisionInfo(
    val deviceId: Long,
    val deviceSsid: String,
    val provisioningToken: String,
    val serverHost: String,
    val serverPort: Int
)
