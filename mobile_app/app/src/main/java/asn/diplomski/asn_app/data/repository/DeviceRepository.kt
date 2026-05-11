package asn.diplomski.asn_app.data.repository

import android.util.Log
import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.api.AsnApi
import asn.diplomski.asn_app.domain.model.Device
import asn.diplomski.asn_app.domain.model.DeviceProvisionInfo
import asn.diplomski.asn_app.domain.model.ProvisionDevice
import asn.diplomski.asn_app.domain.model.Sensor
import asn.diplomski.asn_app.domain.model.TenantProfile
import javax.inject.Inject
import javax.inject.Singleton

private const val TAG = "DeviceRepository"

@Singleton
class DeviceRepository @Inject constructor(
    private val api: AsnApi,
    private val tokenManager: TokenManager
) {
    suspend fun getDevices(token: String?): Result<List<Device>> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
        if (authHeader == null) {
            Log.w(TAG, "getDevices: no token available")
            return Result.failure(Exception("No token available"))
        }
        return try {
            Log.d(TAG, "getDevices: fetching /api/tenants/me")
            val tenantResponse = api.getTenant(authHeader)
            val devices = tenantResponse.devices.map { device ->
                Device(
                    id = device.id,
                    type = device.type,
                    provisionStatus = device.provisionStatus ?: "NotProvisioned",
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

    suspend fun getDeviceProvisionStatus(deviceId: Long, token: String?): Result<String> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
            ?: return Result.failure(Exception("No token available"))
        return try {
            val response = api.getProvisioningConfig(authHeader)
            val device = response.devices.find { it.deviceId == deviceId }
                ?: return Result.failure(Exception("Device $deviceId not found in provisioning response"))
            Result.success(device.provisionStatus)
        } catch (e: Exception) {
            Log.e(TAG, "getDeviceProvisionStatus: failed", e)
            Result.failure(e)
        }
    }

    suspend fun getAllProvisionDevices(token: String?): Result<List<ProvisionDevice>> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
            ?: return Result.failure(Exception("No token available"))
        return try {
            val response = api.getProvisioningConfig(authHeader)
            val devices = response.devices.map { dto ->
                ProvisionDevice(
                    deviceId = dto.deviceId,
                    deviceType = dto.deviceType,
                    deviceSsid = dto.deviceSsid,
                    provisionStatus = dto.provisionStatus
                )
            }
            Result.success(devices)
        } catch (e: Exception) {
            Log.e(TAG, "getAllProvisionDevices: failed", e)
            Result.failure(e)
        }
    }

    suspend fun getTenantProfile(token: String?): Result<TenantProfile> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
            ?: return Result.failure(Exception("No token available"))
        return try {
            val response = api.getTenant(authHeader)
            Result.success(
                TenantProfile(
                    id = response.id,
                    name = response.name,
                    email = response.email,
                    plan = response.plan,
                    phoneNumber = response.phoneNumber,
                    contactPersonName = response.contactPersonName,
                    street = response.street,
                    city = response.city,
                    postalCode = response.postalCode,
                    country = response.country
                )
            )
        } catch (e: Exception) {
            Log.e(TAG, "getTenantProfile: failed", e)
            Result.failure(e)
        }
    }

    suspend fun startProvision(token: String?): Result<Unit> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
            ?: return Result.failure(Exception("No token available"))
        return try {
            Log.d(TAG, "startProvision: POST /api/Provision/start")
            api.startProvision(authHeader)
            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "startProvision: failed", e)
            Result.failure(e)
        }
    }

    suspend fun stopProvision(token: String?): Result<Unit> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
            ?: return Result.failure(Exception("No token available"))
        return try {
            Log.d(TAG, "stopProvision: POST /api/Provision/stop")
            api.stopProvision(authHeader)
            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "stopProvision: failed", e)
            Result.failure(e)
        }
    }

    suspend fun getDeviceProvisionInfo(deviceId: Long, token: String?): Result<DeviceProvisionInfo> {
        val authHeader = tokenManager.getAuthorizationHeader(token)
        if (authHeader == null) {
            Log.w(TAG, "getDeviceProvisionInfo: no token available")
            return Result.failure(Exception("No token available"))
        }
        return try {
            Log.d(TAG, "getDeviceProvisionInfo: fetching /api/provision for deviceId=$deviceId")
            val response = api.getProvisioningConfig(authHeader)
            val device = response.devices.find { it.deviceId == deviceId }
                ?: return Result.failure(Exception("Device $deviceId not found in provisioning response"))
            val info = DeviceProvisionInfo(
                deviceId = device.deviceId,
                deviceSsid = device.deviceSsid,
                provisioningToken = device.provisioningToken,
                serverHost = response.serverHost,
                serverPort = response.serverPort
            )
            Log.d(TAG, "getDeviceProvisionInfo: ssid=${info.deviceSsid}, host=${info.serverHost}:${info.serverPort}")
            Result.success(info)
        } catch (e: Exception) {
            Log.e(TAG, "getDeviceProvisionInfo: failed", e)
            Result.failure(e)
        }
    }
}
