package asn.diplomski.asn_app.domain.model

data class Device(
    val id: Long,
    val type: Int,
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

data class ProvisionConfig(
    val mqttHost: String,
    val mqttPort: Int,
    val token: String,
    val expiresAt: String
)
