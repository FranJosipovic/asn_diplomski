package asn.diplomski.asn_app.data.api.models

data class ProvisionResponse(
    val mqttHost: String,
    val mqttPort: Int,
    val token: String,
    val expiresIn: Long
)
