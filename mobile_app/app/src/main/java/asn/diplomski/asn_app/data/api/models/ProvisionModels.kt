package asn.diplomski.asn_app.data.api.models

data class ProvisioningTokenResponse(
    val provisioningToken: String,
    val expiresAt: String,
    val mqttHost: String,
    val mqttPort: Int
)
