package asn.diplomski.asn_app.data.repository

import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.api.AsnApi
import asn.diplomski.asn_app.data.api.models.SignInRequest
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val api: AsnApi,
    private val tokenManager: TokenManager
) {
    suspend fun signIn(email: String, password: String): Result<Long> {
        return try {
            val response = api.signIn(SignInRequest(email, password))
            tokenManager.saveToken(response.token)

            val tenantId = tokenManager.getTenantIdFromToken(response.token)
            if (tenantId != null) {
                Result.success(tenantId)
            } else {
                Result.failure(Exception("Failed to extract tenant ID from token"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun logout() {
        tokenManager.clearToken()
    }
}
