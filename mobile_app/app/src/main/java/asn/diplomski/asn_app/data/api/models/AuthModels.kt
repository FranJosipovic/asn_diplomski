package asn.diplomski.asn_app.data.api.models

data class SignInRequest(
    val email: String,
    val password: String
)

data class SignInResponse(
    val token: String
)
