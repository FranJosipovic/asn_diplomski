package asn.diplomski.asn_app.domain.model

data class Device(
    val id: Long,
    val type: String,
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
    val type: String,
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

data class ProvisionDevice(
    val deviceId: Long,
    val deviceType: String,
    val deviceSsid: String,
    val provisionStatus: String
)

data class TenantProfile(
    val id: Long,
    val name: String,
    val email: String,
    val plan: String,
    val phoneNumber: String?,
    val contactPersonName: String?,
    val street: String?,
    val city: String?,
    val postalCode: String?,
    val country: String?
)
