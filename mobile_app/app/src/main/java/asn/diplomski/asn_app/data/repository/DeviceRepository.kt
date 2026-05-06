package asn.diplomski.asn_app.data.repository

import android.util.Log
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.api.AsnApi
import asn.diplomski.asn_app.domain.model.Device
import asn.diplomski.asn_app.domain.model.Sensor
import asn.diplomski.asn_app.domain.model.ProvisionConfig
import javax.inject.Inject
import javax.inject.Singleton

private const val TAG = "DeviceRepository"

@Singleton
class DeviceRepository @Inject constructor(
    private val api: AsnApi,
    private val tokenManager: TokenManager
) {
    suspend fun getDevices(tenantId: Long, token: String?): Result<List<Device>> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
        if (authHeader == null) {
            Log.w(TAG, "getDevices: no token available")
            return Result.failure(Exception("No token available"))
        }
        return try {
            Log.d(TAG, "getDevices: fetching tenant $tenantId")
            val tenantResponse = api.getTenant(tenantId, authHeader)
            val devices = tenantResponse.devices.map { device ->
                Device(
                    id = device.id,
                    type = device.type,
                    deviceNumber = device.deviceNumber,
                    description = device.description,
                    isActive = device.isActive,
                    createdAt = device.createdAt,
                    lastSeenAt = device.lastSeenAt,
                    sensors = device.sensors.map { sensor ->
                        Sensor(
                            id = sensor.id,
                            type = sensor.type,
                            sensorNumber = sensor.sensorNumber,
                            description = sensor.description,
                            isActive = sensor.isActive,
                            createdAt = sensor.createdAt
                        )
                    }
                )
            }
            Log.d(TAG, "getDevices: got ${devices.size} devices")
            Result.success(devices)
        } catch (e: Exception) {
            Log.e(TAG, "getDevices: failed", e)
            Result.failure(e)
        }
    }

    suspend fun getProvisionConfig(token: String?): Result<ProvisionConfig> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
        if (authHeader == null) {
            Log.w(TAG, "getProvisionConfig: no token available")
            return Result.failure(Exception("No token available"))
        }
        return try {
            Log.d(TAG, "getProvisionConfig: fetching provisioning config")
            val response = api.getProvisioningConfig(authHeader)
            val config = ProvisionConfig(
                mqttHost = response.mqttHost,
                mqttPort = response.mqttPort,
                token = response.provisioningToken,
                expiresAt = response.expiresAt
            )
            Log.d(TAG, "getProvisionConfig: mqtt=${config.mqttHost}:${config.mqttPort}, expires=${config.expiresAt}")
            Result.success(config)
        } catch (e: Exception) {
            Log.e(TAG, "getProvisionConfig: failed", e)
            Result.failure(e)
        }
    }
}
