package asn.diplomski.asn_app.data

import android.content.Context
import android.util.Base64
import dagger.hilt.android.qualifiers.ApplicationContext
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.flow.map
import org.json.JSONObject
import javax.inject.Inject
import javax.inject.Singleton

private val Context.dataStore by preferencesDataStore(name = "auth")

@Singleton
class TokenManager @Inject constructor(
    @ApplicationContext private val context: Context
) {
    private val ACCESS_TOKEN_KEY = stringPreferencesKey("access_token")
    private val REFRESH_TOKEN_KEY = stringPreferencesKey("refresh_token")

    val tokenFlow: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[ACCESS_TOKEN_KEY]
    }

    suspend fun saveTokens(accessToken: String, refreshToken: String, refreshTokenExpiresAt: String) {
        context.dataStore.edit { preferences ->
            preferences[ACCESS_TOKEN_KEY] = accessToken
            preferences[REFRESH_TOKEN_KEY] = refreshToken
        }
    }

    suspend fun clearTokens() {
        context.dataStore.edit { preferences ->
            preferences.remove(ACCESS_TOKEN_KEY)
            preferences.remove(REFRESH_TOKEN_KEY)
        }
    }

    suspend fun getRefreshToken(): String? {
        return context.dataStore.data.map { preferences ->
            preferences[REFRESH_TOKEN_KEY]
        }.firstOrNull()
    }

    fun getTenantIdFromToken(token: String?): Long? {
        if (token == null) return null
        return try {
            val parts = token.split(".")
            if (parts.size != 3) return null

            val decoder = Base64.decode(parts[1] + "==", Base64.DEFAULT)
            val payload = String(decoder, Charsets.UTF_8)
            val jsonPayload = JSONObject(payload)

            jsonPayload.optString("sub").toLongOrNull()
                ?: jsonPayload.optLong("tenant_id").takeIf { it > 0 }
        } catch (e: Exception) {
            null
        }
    }

    fun getAuthorizationHeader(token: String?): String? {
        return if (token != null) "Bearer $token" else null
    }
}
