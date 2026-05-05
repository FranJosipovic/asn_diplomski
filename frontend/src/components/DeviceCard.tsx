import { DeviceType, deviceTypeLabel, sensorTypeClass, sensorTypeLabel, timeAgo, isRecentlySeen, formatDateTime, SensorType } from '../types'
import type { DeviceResponse, SensorResponse } from '../types'

interface Props {
  device: DeviceResponse
  onSensorClick?: (sensor: SensorResponse) => void
}

export default function DeviceCard({ device, onSensorClick }: Props) {
  const isSensor = device.type === DeviceType.SensorUnit
  const recently = isRecentlySeen(device.lastSeenAt)

  return (
    <div className="device-card">
      <div className="device-card-top">
        <span className={`device-type-tag ${isSensor ? 'sensor' : 'pump'}`}>
          {deviceTypeLabel(device.type)}
        </span>
        <div className="row gap-8">
          <span className={`badge ${device.isActive ? 'badge-active' : 'badge-offline'}`}>
            {device.isActive ? 'Active' : 'Offline'}
          </span>
          <span className="device-number">#{device.deviceNumber}</span>
        </div>
      </div>

      <div className="device-card-body">
        <div className="device-meta">
          <div className="device-meta-item">
            <span>ID</span>
            <span style={{ color: 'var(--text-1)' }}>{device.id}</span>
          </div>
          <div className="device-meta-item">
            <div className={`seen-dot ${recently ? 'live' : ''}`} />
            <span>
              {device.lastSeenAt
                ? `${recently ? 'LIVE · ' : ''}${timeAgo(device.lastSeenAt)}`
                : 'never seen'}
            </span>
          </div>
          {device.lastSeenAt && (
            <div className="device-meta-item" title={formatDateTime(device.lastSeenAt)}>
              <span>{formatDateTime(device.lastSeenAt)}</span>
            </div>
          )}
        </div>

        <div className="sensors-label">Sensors ({device.sensors.length})</div>
        {device.sensors.length === 0 ? (
          <div className="empty" style={{ padding: '16px 0', textAlign: 'left' }}>
            No sensors provisioned
          </div>
        ) : (
          <div>
            {device.sensors.map(s => {
              const isReadable = s.type === SensorType.Temperature || s.type === SensorType.SoilMoisture
              return (
                <button
                  key={s.id}
                  className={`sensor-row ${isReadable ? 'readable' : ''}`}
                  onClick={() => isReadable && onSensorClick?.(s)}
                  style={{ cursor: isReadable ? 'pointer' : 'default' }}
                >
                  <div className="sensor-left">
                    <div className={`sensor-pip ${sensorTypeClass(s.type)}`} />
                    <span className="sensor-name">{sensorTypeLabel(s.type)}</span>
                  </div>
                  <span className="sensor-id" style={{marginLeft:"5px"}}>ID {s.id} · #{s.sensorNumber}</span>
                </button>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
