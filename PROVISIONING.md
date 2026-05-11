# Device Provisioning & System Control

## Overview

Provisioning connects a physical ESP32 device to the backend so it can receive commands and publish sensor data. The full lifecycle from factory-fresh to actively irrigating involves three actors:

- **Mobile app** — drives initial setup, polls status, starts/stops the system
- **Server** — issues tokens, exposes HTTP endpoints, subscribes to MQTT topics
- **Device** — connects to WiFi, exchanges the provisioning token, joins MQTT, responds to commands

---

## Device State Machine

```
NotProvisioned
      │
      │  GET /api/provision called by mobile app
      ▼
ProvisioningReady
      │
      │  Device POSTs token → POST /api/provision
      ▼
Provisioning
      │
      │  Device confirms MQTT → POST /api/provision/confirm
      ▼
Ready ◄────────────────────────────────────────────┐
      │                                             │
      │  Device receives {"command":"start"}        │
      │  Device publishes {"event":"started"}       │
      ▼                                             │
Working                                             │
      │                                             │
      │  Device receives {"command":"stop"}         │
      │  Device publishes {"event":"stopped"}       │
      ▼                                             │
Stopped ─────────────────────────────────────────── ┘
                  (start restarts from Stopped)
```

| State | Who sets it | Condition |
|---|---|---|
| `NotProvisioned` | Initial / factory reset | Device has never been provisioned |
| `ProvisioningReady` | Server | `GET /api/provision` called; token generated |
| `Provisioning` | Server | Device exchanged token via `POST /api/provision` |
| `Ready` | Server | Device confirmed MQTT via `POST /api/provision/confirm` |
| `Working` | Server | Received `{"event":"started"}` on MQTT status topic |
| `Stopped` | Server | Received `{"event":"stopped"}` on MQTT status topic |

---

## MQTT Topic Reference

| Direction | Topic | Payload |
|---|---|---|
| Server → Device | `tenant_{id}/device_{id}/command` | `{"command":"start"}` or `{"command":"stop"}` |
| Device → Server | `tenant_{id}/device_{id}/sensor_{sensorId}/temperature` | `{"value":23.5,"unit":"°C","timestamp":"..."}` |
| Device → Server | `tenant_{id}/device_{id}/sensor_{sensorId}/soil` | `{"value":62,"unit":"%","timestamp":"..."}` |
| Device → Server | `tenant_{id}/device_{id}/sensor_{sensorId}/water-level` | `{"value":85,"unit":"%","timestamp":"..."}` |
| Device → Server | `tenant_{id}/device_{id}/status` | `{"event":"started"}` or `{"event":"stopped"}` |

The server subscribes to all device topics at `POST /api/provision/confirm` time. On MQTT broker reconnect, it automatically re-subscribes to all previously registered topics.

---

## Step-by-step

### Step 1 — Mobile app: fetch provisioning data

**`GET /api/provision`** — requires Bearer JWT

```json
{
  "expiresAt": "2025-01-01T00:15:00Z",
  "serverHost": "192.168.1.100",
  "serverPort": 5000,
  "devices": [
    {
      "deviceId": 2,
      "deviceType": "SensorUnit",
      "deviceSsid": "SensorUnit_2",
      "provisionStatus": "NotProvisioned",
      "provisioningToken": "abc123...",
      "sensors": [
        { "sensorId": 3, "sensorType": "Temperature" },
        { "sensorId": 4, "sensorType": "SoilMoisture" }
      ]
    },
    {
      "deviceId": 5,
      "deviceType": "PumpUnit",
      "deviceSsid": "PumpUnit_5",
      "provisionStatus": "NotProvisioned",
      "provisioningToken": "xyz789...",
      "sensors": [...]
    }
  ]
}
```

**What happens on the server:** for each device with status `NotProvisioned` (or `ProvisioningReady` with an expired token), a new 64-byte cryptographic provisioning token is generated and stored with a 15-minute expiry. Device status is set to `ProvisioningReady`.

**Mobile app rules:**
- Only provision devices where `provisionStatus == "NotProvisioned"` or `"ProvisioningReady"`. Skip `"Provisioning"`, `"Ready"`, `"Working"`, `"Stopped"` — those are already configured.
- Save `serverHost` and `serverPort` — these are sent to the device so it knows where to call back.
- Token expires in 15 minutes (`expiresAt`). If it expires before you finish, call this endpoint again.

---

### Step 2 — Mobile app: connect to device SoftAP

For each device that needs provisioning:

1. Disconnect phone from current WiFi
2. Connect to the device's SoftAP — SSID = `deviceSsid` from the response (e.g. `SensorUnit_2`), no password
3. The device runs an HTTP server at `192.168.4.1` (ESP-IDF SoftAP default)

**Important:** provisioning is one-device-at-a-time. If you have two devices, repeat steps 2–4 for each.

---

### Step 3 — Mobile app: send provisioning data to device

Two things must be sent while connected to the SoftAP:

**a) Backend token + server address** — via custom `prov-data` endpoint (send this **first**):

POST to `http://192.168.4.1/prov-data`:
```json
{
  "token": "abc123...",
  "host": "192.168.1.100",
  "port": 5000
}
```

Use the Espressif provisioning SDK (`sendDataToCustomEndpoint`) to reach this endpoint. Bind the HTTP client to the WiFi network interface explicitly — Android may route through cellular otherwise.

**b) WiFi credentials** — via the ESP-IDF provisioning protocol (SoftAP scheme, Security 0):
- Use the Espressif `provisioning` Android SDK (`ESPProvisionManager`)
- Send the home/office WiFi SSID + password the device should connect to

**Order matters:** send `prov-data` **before** sending WiFi credentials. The device reboots once WiFi credentials are applied — if the token isn't saved yet, it'll be lost.

---

### Step 4 — Device takes over automatically (no app interaction needed)

Once the device has both the token and WiFi credentials:

1. Device connects to home WiFi
2. Device POSTs `{"token":"abc123..."}` to `POST /api/provision` → server returns MQTT credentials and sensor IDs → device status becomes `Provisioning`
3. Device connects to MQTT broker
4. Device POSTs `{"tenantId":1,"deviceId":2}` to `POST /api/provision/confirm` → server registers all MQTT subscriptions → device status becomes `Ready`
5. Device LED turns solid blue — ready and waiting for a start command

The mobile app should poll during this phase to show progress.

---

### Step 5 — Mobile app: poll until device is Ready

Call `GET /api/provision` every ~3–5 seconds.

Watch the `provisionStatus` field on each device:

| Status | UI |
|---|---|
| `ProvisioningReady` | Waiting for device to connect to WiFi |
| `Provisioning` | Device connected to WiFi, joining MQTT |
| `Ready` | Device fully configured — provisioning complete |

**Timeout:** if a device stays in `Provisioning` for more than 2 minutes, show an error. The device likely can't reach the MQTT broker (check firewall, broker address).

---

### Step 6 — Mobile app: start the system

**`POST /api/provision/start`** — requires Bearer JWT, no body

```json
{
  "notifiedDeviceCount": 2,
  "notifiedDeviceIds": [2, 5]
}
```

This publishes `{"command":"start"}` via MQTT to all devices with status `Ready`, `Working`, or `Stopped`. Each device:
1. Sets `systemStarted = true`
2. Switches LED to blinking green
3. Publishes `{"event":"started"}` to its status topic
4. Server receives event → transitions device to `Working`

Only call this once all devices are `Ready`.

---

### Step 7 — Stop the system

**`POST /api/provision/stop`** — requires Bearer JWT, no body

```json
{
  "notifiedDeviceCount": 2,
  "notifiedDeviceIds": [2, 5]
}
```

Publishes `{"command":"stop"}` to all `Ready`, `Working`, and `Stopped` devices. Each device:
1. Sets `systemStarted = false`
2. Switches LED to solid red
3. Publishes `{"event":"stopped"}` to its status topic
4. Server receives event → transitions device to `Stopped`

Calling `POST /api/provision/start` after this restarts the system — `Stopped` devices are included in the start target.

---

## API Quick Reference

| Method | Path | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/provision` | JWT | Fetch provisioning data + tokens for all devices |
| `POST` | `/api/provision` | None | Device exchanges provisioning token for MQTT config |
| `POST` | `/api/provision/confirm` | None | Device confirms MQTT connection; server subscribes |
| `POST` | `/api/provision/start` | JWT | Send start command to all commandable devices |
| `POST` | `/api/provision/stop` | JWT | Send stop command to all commandable devices |

### POST /api/provision — Device → Server

Request body (device sends this):
```json
{ "token": "abc123..." }
```

Response:
```json
{
  "tenantId": 1,
  "deviceId": 2,
  "mqttHost": "192.168.1.100",
  "mqttPort": 1883,
  "sensors": [
    { "sensorId": 3, "sensorType": "Temperature" },
    { "sensorId": 4, "sensorType": "SoilMoisture" }
  ]
}
```

Error responses: `404` token not found, `410` token expired.

### POST /api/provision/confirm — Device → Server

Request body:
```json
{ "tenantId": 1, "deviceId": 2 }
```

Response: `204 No Content`

Error responses: `404` device not found, `409` device not in `Provisioning` state.

---

## Device LED Reference

| LED | State | Meaning |
|---|---|---|
| Solid orange | `NotProvisioned` | No WiFi credentials stored |
| Blinking purple | Provisioning SoftAP | Waiting for mobile app |
| Blinking blue | `ProvisioningReady` / `Provisioning` | Talking to backend or joining MQTT |
| Solid blue | `Ready` | MQTT connected, waiting for start |
| Blinking green | `Working` | Sending sensor data |
| Solid red | `Stopped` | Received stop command |

---

## Error Handling

| Situation | What to do |
|---|---|
| Token expired before provisioning | Re-call `GET /api/provision` for fresh tokens |
| Device SoftAP not found | Power-cycle device; retry |
| `prov-data` POST fails | Retry; or send again before WiFi credentials |
| Device stays `Provisioning` > 2 min | MQTT broker unreachable — check broker IP and firewall |
| `start` returns 0 notified devices | No devices in commandable state; check device status |
| Device LED stays orange after reset | Provisioning failed; hold RESET_PIN to factory-reset |

---

## Re-provisioning

To move a device to a new WiFi network:
1. Hold RESET_PIN (GPIO 0) low on boot — device clears all stored config and restarts
2. Backend keeps the device record with its sensors intact
3. Call `GET /api/provision` — new token is generated, status resets to `ProvisioningReady`
4. Repeat provisioning from Step 2

---

## Security Notes

- Provisioning token is sent as plain HTTP over the SoftAP connection (open network). Anyone nearby could sniff it during the ~30 second window. Acceptable for a thesis/lab environment; production would use `NETWORK_PROV_SECURITY_1` (SRP encryption).
- Confirm and complete-provisioning endpoints are `[AllowAnonymous]` by design — the device has no JWT. The provisioning token itself acts as the credential.
