# Device Provisioning Flow

## Overview

Provisioning connects an ESP device to the system in three stages:

```
NOT_PROVISIONED → (WiFi + token exchange) → PROVISIONING → (MQTT confirmed) → PROVISIONED
```

The mobile app drives the first stage. The device drives the rest automatically.

---

## Prerequisites

- User is signed in → has a valid JWT access token
- ESP device is powered on and **never provisioned before** (or was factory-reset via RESET_PIN)
- ESP is broadcasting a SoftAP with SSID matching its `deviceSsid` (e.g. `SensorUnit_9`)

---

## Step-by-step

### 1. Fetch provisioning data — `GET /api/provision`

**Auth:** Bearer JWT

**Response:**
```json
{
  "expiresAt": "2025-01-01T00:15:00Z",
  "serverHost": "192.168.1.100",
  "serverPort": 5000,
  "devices": [
    {
      "deviceId": 2,
      "deviceType": "SensorUnit",
      "deviceSsid": "SensorUnit_9",
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
      "deviceSsid": "PumpUnit_1",
      "provisionStatus": "NotProvisioned",
      "provisioningToken": "xyz789...",
      "sensors": [...]
    }
  ]
}
```

**Notes:**
- Tokens expire after **15 minutes** (`expiresAt`). If expired, call this endpoint again to get fresh tokens.
- Only provision devices where `provisionStatus == "NotProvisioned"`. Skip `"Provisioning"` or `"Provisioned"` devices.
- `serverHost` + `serverPort` is what you send to the device so it knows where to call back.

---

### 2. For each device — connect to its SoftAP

- Disconnect phone from current WiFi
- Connect to the device's SoftAP: SSID = `deviceSsid` from the response (e.g. `SensorUnit_9`), no password
- The device runs an HTTP server on `192.168.4.1` (ESP-IDF SoftAP default)

---

### 3. Send provisioning data to the device

Two things must be sent to the device while connected to its SoftAP:

**a) WiFi credentials** — via ESP-IDF provisioning protocol (BLE or SoftAP scheme)
- Use the Espressif `provisioning` Android SDK
- Send the home/office WiFi SSID + password the device should connect to

**b) Backend token + server address** — via custom `prov-data` endpoint
- POST to `http://192.168.4.1/proto-ver` first to verify connection (optional, SDK handles this)
- Send custom data via the `prov-data` endpoint (SDK: `sendDataToCustomEndpoint`)

Payload to send to `prov-data`:
```json
{
  "token": "abc123...",
  "host": "192.168.1.100",
  "port": 5000
}
```

**Order matters:** send the `prov-data` custom data **before** sending WiFi credentials, otherwise the device may reboot and connect to WiFi before saving the token.

---

### 4. Device takes over automatically

Once it receives both:
1. Device connects to WiFi
2. Device POSTs `token` to `POST /api/provision/{token}` → receives MQTT credentials → status becomes `Provisioning`
3. Device connects to MQTT broker
4. Device POSTs `POST /api/provision/confirm` → status becomes `Provisioned`

The app doesn't need to do anything during this phase — just wait and poll.

---

### 5. Poll until all devices are Provisioned

Call `GET /api/provision` every ~3 seconds until all devices show `provisionStatus == "Provisioned"`.

Timeout after ~2 minutes — if a device is still `Provisioning` after that, show an error (device may have failed to connect to MQTT).

---

### 6. Start the system — `POST /api/provision/start`

**Auth:** Bearer JWT — no request body needed.

**Response:**
```json
{
  "notifiedDeviceCount": 2,
  "notifiedDeviceIds": [2, 5]
}
```

This publishes a `{ "command": "start" }` MQTT message to every `Provisioned` device. The devices begin their sensor loop immediately.

---

## State reference

| Status | Meaning |
|---|---|
| `NotProvisioned` | Fresh device, never configured |
| `Provisioning` | Device has MQTT credentials, connecting to broker |
| `Provisioned` | Device confirmed MQTT connection, ready to start |

---

## Error cases to handle in the app

| Situation | What to do |
|---|---|
| Token expired before provisioning | Re-call `GET /api/provision` to get fresh tokens |
| Device SoftAP not found | Ask user to power-cycle device, retry |
| `prov-data` send fails | Retry, or restart provisioning for that device |
| Device stuck in `Provisioning` after 2 min | Check if MQTT broker is reachable; show error |
| `POST /api/provision/start` returns 0 notified devices | No devices are `Provisioned` yet, wait and retry |

---

## Things potentially missing / worth discussing

- **Provisioning multiple devices** — current flow provisions one device at a time (phone connects to one SoftAP). If there are 2 devices, you repeat steps 2–4 for each. There is no parallel provisioning.

- **Re-provisioning a device** — if a device needs to move to a new WiFi network, it must be factory-reset (hold RESET_PIN low on boot). The backend keeps the device record; a new provisioning token is generated on the next `GET /api/provision` call. The backend should probably reset the device's `provisionStatus` back to `NotProvisioned` when a new token is issued.

- **`serverHost` reliability** — the app sends its own IP (`serverHost` from `GET /api/provision` response) to the device. If the backend is behind NAT or the phone is on a different network than the backend this will break. Make sure `serverHost` is the LAN IP of the server, not `localhost`.

- **Token is sent in plaintext over SoftAP** — the SoftAP connection is open (no WiFi password) and the HTTP call to `prov-data` is plain HTTP. Anyone nearby could sniff the provisioning token. For a thesis project this is fine; for production you'd use `NETWORK_PROV_SECURITY_1` (SRP-based encryption).

- **No feedback from device to app during stage 4** — the app can only infer progress by polling `GET /api/provision`. A WebSocket or SSE push from the backend when status changes would give a smoother UX.

- **`POST /api/provision/start` starts all provisioned devices** — there is no way to start a single device. If one device provisioned and another didn't, calling start will start the first one. Consider whether that's the desired behavior or if start should require all devices to be provisioned first.
