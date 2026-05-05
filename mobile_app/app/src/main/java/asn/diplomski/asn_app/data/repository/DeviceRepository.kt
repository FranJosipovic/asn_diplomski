package asn.diplomski.asn_app.data.repository

import asn.diplomski.asn_app.data.TokenManager
import asn.diplomski.asn_app.data.api.AsnApi
import asn.diplomski.asn_app.domain.model.Device
import asn.diplomski.asn_app.domain.model.Sensor
import asn.diplomski.asn_app.domain.model.ProvisionConfig
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class DeviceRepository @Inject constructor(
    private val api: AsnApi,
    private val tokenManager: TokenManager
) {
    fun getDevices(tenantId: Long, token: String?): Flow<Result<List<Device>>> = flow {
        try {
            val authHeader = tokenManager.getAuthorizationHeader(token)
            if (authHeader == null) {
                emit(Result.failure(Exception("No token available")))
                return@flow
            }

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
            emit(Result.success(devices))
        } catch (e: Exception) {
            emit(Result.failure(e))
        }
    }

    fun getProvisionConfig(token: String?): Flow<Result<ProvisionConfig>> = flow {
        try {
            val authHeader = tokenManager.getAuthorizationHeader(token)
            if (authHeader == null) {
                emit(Result.failure(Exception("No token available")))
                return@flow
            }

            val response = api.getProvisionConfig(authHeader)
            val config = ProvisionConfig(
                mqttHost = response.mqttHost,
                mqttPort = response.mqttPort,
                token = response.token,
                expiresIn = response.expiresIn
            )
            emit(Result.success(config))
        } catch (e: Exception) {
            emit(Result.failure(e))
        }
    }
}
