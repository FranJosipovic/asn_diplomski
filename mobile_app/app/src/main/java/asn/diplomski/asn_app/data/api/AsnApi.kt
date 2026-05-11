package asn.diplomski.asn_app.data.api

import asn.diplomski.asn_app.data.api.models.ProvisioningTokenResponse
import asn.diplomski.asn_app.data.api.models.RefreshTokenRequest
import asn.diplomski.asn_app.data.api.models.RefreshTokenResponse
import asn.diplomski.asn_app.data.api.models.SignInRequest
import asn.diplomski.asn_app.data.api.models.SignInResponse
import asn.diplomski.asn_app.data.api.models.TenantResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface AsnApi {
    @POST("/api/auth/signin")
    suspend fun signIn(@Body request: SignInRequest): SignInResponse

    @POST("/api/auth/refresh")
    suspend fun refreshToken(@Body request: RefreshTokenRequest): RefreshTokenResponse

    @GET("/api/tenants/me")
    suspend fun getTenant(
        @Header("Authorization") token: String
    ): TenantResponse

    @POST("/api/tenants/{id}/connect")
    suspend fun connectTenant(
        @Path("id") tenantId: Long,
        @Header("Authorization") token: String
    ): Map<String, Any>

    @GET("/api/provision")
    suspend fun getProvisioningConfig(
        @Header("Authorization") token: String
    ): ProvisioningTokenResponse

    @POST("/api/Provision/start")
    suspend fun startProvision(
        @Header("Authorization") token: String
    ): Map<String, Any>

    @POST("/api/Provision/stop")
    suspend fun stopProvision(
        @Header("Authorization") token: String
    ): Map<String, Any>
}
