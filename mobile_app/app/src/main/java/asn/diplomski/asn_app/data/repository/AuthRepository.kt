package asn.diplomski.asn_app.data.repository

import android.util.Log
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.api.AsnApi
import asn.diplomski.asn_app.data.api.models.RefreshTokenRequest
import asn.diplomski.asn_app.data.api.models.SignInRequest
import javax.inject.Inject
import javax.inject.Singleton

private const val TAG = "AuthRepository"

@Singleton
class AuthRepository @Inject constructor(
    private val api: AsnApi,
    private val tokenManager: TokenManager
) {
    suspend fun signIn(email: String, password: String): Result<Long> {
        Log.d(TAG, "signIn: email=$email")
        return try {
            val response = api.signIn(SignInRequest(email, password))
            tokenManager.saveTokens(response.accessToken, response.refreshToken, response.refreshTokenExpiresAt)
            Log.d(TAG, "signIn: success, tenantId=${response.tenantId}")
            Result.success(response.tenantId)
        } catch (e: Exception) {
            Log.e(TAG, "signIn: failed", e)
            Result.failure(e)
        }
    }

    suspend fun refreshToken(): Result<Unit> {
        Log.d(TAG, "refreshToken: attempting token refresh")
        return try {
            val refreshToken = tokenManager.getRefreshToken()
            if (refreshToken == null) {
                Log.w(TAG, "refreshToken: no refresh token stored")
                return Result.failure(Exception("No refresh token available"))
            }
            val response = api.refreshToken(RefreshTokenRequest(refreshToken))
            tokenManager.saveTokens(response.accessToken, response.refreshToken, response.refreshTokenExpiresAt)
            Log.d(TAG, "refreshToken: success")
            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "refreshToken: failed", e)
            Result.failure(e)
        }
    }

    suspend fun logout() {
        Log.d(TAG, "logout: clearing tokens")
        tokenManager.clearTokens()
    }
}
