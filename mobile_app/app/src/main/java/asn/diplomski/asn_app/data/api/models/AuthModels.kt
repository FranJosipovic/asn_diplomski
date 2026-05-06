package asn.diplomski.asn_app.data.api.models

data class SignInRequest(
    val email: String,
    val password: String
)

data class SignInResponse(
    val tenantId: Long,
    val email: String,
    val accessToken: String,
    val refreshToken: String,
    val refreshTokenExpiresAt: String
)

data class RefreshTokenRequest(
    val refreshToken: String
)

data class RefreshTokenResponse(
    val accessToken: String,
    val refreshToken: String,
    val refreshTokenExpiresAt: String
)
