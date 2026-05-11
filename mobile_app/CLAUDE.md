# CLAUDE.md — Mobile App

This file provides guidance to Claude Code when working in the `mobile_app/` directory.

## Project Overview

Android app for the **Automatski sustav za navodnjavanje** (Automated Irrigation System). Kotlin + Jetpack Compose. Hilt for DI, Retrofit for networking, Espressif provisioning SDK for ESP32 device setup.

## Build & Run

Open `mobile_app/` in Android Studio. No special gradle commands needed — standard `Run` or `./gradlew assembleDebug`.

The backend base URL is configured in `di/NetworkModule.kt`. During development, replace with your machine's LAN IP (e.g. `http://192.168.1.100:5017`). `localhost` does not work on a physical device.

## Package Structure

```
asn.diplomski.asn_app/
├── data/
│   ├── api/
│   │   ├── AsnApi.kt              — Retrofit interface (all endpoints)
│   │   └── models/                — Raw API response DTOs
│   │       ├── AuthModels.kt
│   │       ├── TenantModels.kt    — TenantResponse, DeviceResponse, SensorResponse
│   │       └── ProvisionModels.kt — ProvisioningTokenResponse, DeviceProvisioningDto
│   ├── repository/
│   │   ├── AuthRepository.kt
│   │   └── DeviceRepository.kt   — getDevices, getDeviceProvisionInfo, startProvision, stopProvision
│   └── TokenManager.kt            — JWT storage and Authorization header builder
├── domain/
│   └── model/Models.kt            — Device, Sensor, DeviceProvisionInfo (domain objects)
├── di/
│   └── NetworkModule.kt           — Hilt: Retrofit, OkHttpClient, AsnApi
└── ui/
    ├── auth/                       — AuthScreen + AuthViewModel
    ├── provisioning/               — ProvisioningScreen + ProvisioningViewModel
    ├── devices/                    — DevicesScreen + DevicesViewModel
    └── navigation/Navigation.kt
```

## API Endpoints Used

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/auth/signin` | Sign in, get JWT |
| POST | `/api/auth/refresh` | Refresh access token |
| GET | `/api/tenants/me` | Get tenant + devices + sensors |
| GET | `/api/provision` | Get provisioning tokens for all devices |
| POST | `/api/provision/start` | Send start command to all ready devices |
| POST | `/api/provision/stop` | Send stop command to all active devices |

All endpoints except signin/refresh require `Authorization: Bearer <token>` header.

## Device Status Values

The `provisionStatus` field on `DeviceResponse` (from `GET /api/tenants/me`) and `DeviceProvisioningDto` (from `GET /api/provision`) carries one of these string values:

| Value | Meaning | UI action |
|---|---|---|
| `"NotProvisioned"` | Fresh device | Show "Provision" button |
| `"ProvisioningReady"` | Token issued, waiting for device | Skip provisioning flow |
| `"Provisioning"` | Device has MQTT credentials, connecting | Show spinner |
| `"Ready"` | Device confirmed MQTT, ready to start | Show "Start" button |
| `"Working"` | Actively sending sensor data | Show "Stop" button |
| `"Stopped"` | Received stop command | Show "Start" button |

**Commandable statuses** (Ready / Working / Stopped): `POST /api/provision/start` and `POST /api/provision/stop` both target all devices in these states. The device decides what to do with a redundant command.

## Provisioning Flow (ProvisioningViewModel)

The full flow is documented in `PROVISIONING.md` at the repo root. Summary of what the ViewModel does:

1. `GET /api/provision` → find device by `deviceId`, extract `deviceSsid`, `provisioningToken`, `serverHost`, `serverPort`
2. Connect to device SoftAP via `ESPProvisionManager` (`TRANSPORT_SOFTAP`, `SECURITY_0`)
3. On connect: POST `{"token":"...","host":"...","port":...}` to `http://192.168.4.1/prov-data` — bind socket to the WiFi network interface or Android will route through cellular
4. Scan networks, present list to user
5. User picks network + enters password → `espDevice.provision(ssid, password, ...)`
6. On `deviceProvisioningSuccess`: poll `GET /api/provision` every 5 s until `provisionStatus == "Ready"` (2-minute timeout)

## Important Notes

- `startProvision(deviceId, token)` in `DeviceRepository` — the `deviceId` parameter is unused (the server starts all commandable devices). It's kept for call-site compatibility but can be removed.
- The Espressif SDK delivers events via `EventBus` (GreenRobot). `ProvisioningViewModel` registers/unregisters in `init`/`onCleared`.
- The HTTP call to `192.168.4.1/prov-data` must be sent **before** WiFi credentials to avoid a race where the device reboots before saving the token.
- Android may route the `prov-data` request through cellular if not explicitly bound. The ViewModel binds the `OkHttpClient` to the active WiFi network using `ConnectivityManager.allNetworks`.
