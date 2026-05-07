package asn.diplomski.asn_app.data.api.models

data class TenantResponse(
    val id: Long,
    val name: String,
    val email: String,
    val plan: String,
    val isActive: Boolean,
    val createdAt: String,
    val updatedAt: String,
    val phoneNumber: String?,
    val contactPersonName: String?,
    val street: String?,
    val city: String?,
    val postalCode: String?,
    val country: String?,
    val devices: List<DeviceResponse>
)

data class DeviceResponse(
    val id: Long,
    val type: Int,
    val provisionStatus: String?,
    val deviceNumber: Int,
    val description: String?,
    val isActive: Boolean,
    val createdAt: String,
    val lastSeenAt: String?,
    val sensors: List<SensorResponse>
)

data class SensorResponse(
    val id: Long,
    val type: Int,
    val sensorNumber: Int,
    val description: String,
    val isActive: Boolean,
    val createdAt: String
)
