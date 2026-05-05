package asn.diplomski.asn_app.data.api

import asn.diplomski.asn_app.data.api.models.ProvisionResponse
import asn.diplomski.asn_app.data.api.models.SignInRequest
import asn.diplomski.asn_app.data.api.models.SignInResponse
import asn.diplomski.asn_app.data.api.models.TenantResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface AsnApi {
    @POST("/api/auth/signin")
    suspend fun signIn(@Body request: SignInRequest): SignInResponse

    @GET("/api/Tenants/{tenantId}")
    suspend fun getTenant(
        @Path("tenantId") tenantId: Long,
        @Header("Authorization") token: String
    ): TenantResponse

    @GET("/api/provision")
    suspend fun getProvisionConfig(
        @Header("Authorization") token: String
    ): ProvisionResponse
}
