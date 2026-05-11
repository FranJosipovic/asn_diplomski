import { useState } from 'react'
import type { DeviceResponse } from '../types'

interface Props {
  devices: DeviceResponse[]
}

const STEPS = [
  {
    n: 1,
    title: 'Fetch provisioning tokens',
    body: 'Open the mobile app and sign in. Tap "Provision Devices" — the app calls GET /api/provision and generates a 15-minute token for each unprovisionend device.',
  },
  {
    n: 2,
    title: 'Connect to device SoftAP',
    body: 'In your phone\'s Wi-Fi settings, connect to the device\'s access point (e.g. SensorUnit_1 or PumpUnit_2 — no password). Provision one device at a time.',
  },
  {
    n: 3,
    title: 'Send credentials to device',
    body: 'The mobile app sends the provisioning token + server address to the device first, then sends your home/office Wi-Fi credentials via the ESP provisioning protocol. Order matters — token must be sent before Wi-Fi credentials.',
  },
  {
    n: 4,
    title: 'Device connects automatically',
    body: 'The device reboots, joins your Wi-Fi, exchanges the token with the server for MQTT credentials, then confirms its MQTT connection. This is fully automatic — no app interaction needed.',
  },
  {
    n: 5,
    title: 'Wait for Ready status',
    body: 'The mobile app polls every few seconds. Once each device reaches Ready status (solid blue LED), provisioning is complete. Refresh this page to see updated device status.',
  },
]

export default function ProvisioningGuide({ devices }: Props) {
  const [open, setOpen] = useState(false)
  const unprovisionedCount = devices.filter(d => d.provisionStatus !== 'Provisioned').length

  return (
    <div className="provision-banner">
      <div className="provision-banner-header">
        <div className="provision-banner-left">
          <div className="provision-status-dot" />
          <div>
            <div className="provision-title">
              {unprovisionedCount} device{unprovisionedCount !== 1 ? 's' : ''} not provisioned
            </div>
            <div className="provision-subtitle">
              Use the <strong>ASN mobile app</strong> to provision your devices.
              The steps below are for reference.
            </div>
          </div>
        </div>
        <button
          className="btn btn-ghost btn-sm"
          onClick={() => setOpen(v => !v)}
        >
          {open ? 'Hide steps' : 'Show steps'}
        </button>
      </div>

      {open && (
        <div className="provision-steps">
          {STEPS.map(step => (
            <div key={step.n} className="provision-step">
              <div className="provision-step-num">{step.n}</div>
              <div>
                <div className="provision-step-title">{step.title}</div>
                <div className="provision-step-body">{step.body}</div>
              </div>
            </div>
          ))}
          <div className="provision-note">
            Token expires in 15 minutes. If it expires before you finish, re-open the provisioning screen in the mobile app to generate fresh tokens.
          </div>
        </div>
      )}

      <div className="provision-device-list">
        {devices.filter(d => d.provisionStatus !== 'Provisioned').map(d => (
          <div key={d.id} className="provision-device-row">
            <span className="provision-device-name">
              {d.type === 1 ? 'SensorUnit' : 'PumpUnit'} #{d.deviceNumber}
            </span>
            <span className={`badge provision-status-badge status-${d.provisionStatus.toLowerCase()}`}>
              {d.provisionStatus}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}
