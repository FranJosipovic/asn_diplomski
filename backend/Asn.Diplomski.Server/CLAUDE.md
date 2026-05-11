# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Overview

IoT SaaS backend for automated irrigation (diplomski/thesis project). Targets .NET 10. Solution file: `Asn.Diplomski/Asn.Diplomski.Server.slnx` (VS 2022 `.slnx` format).

| Project | Role |
|---|---|
| `Asn.Diplomski.Domain` | Entities and enums only — no dependencies |
| `Asn.Diplomski.Application` | Use-case handlers + repository/MQTT interfaces — depends on Domain |
| `Asn.Diplomski.Rdbm` | EF Core + Npgsql, repository implementations, migrations — depends on Domain + Application |
| `Asn.Diplomski.Server` | ASP.NET Core Web API, controllers, DTOs, MQTT infrastructure — depends on Application + Rdbm |

## Common Commands

Run from `Asn.Diplomski.Server/`:

```powershell
# Build
dotnet build

# Run (dev)
dotnet run

# EF migrations (run from Asn.Diplomski.Rdbm/)
dotnet ef migrations add <Name> --startup-project ..\Asn.Diplomski.Server
dotnet ef database update --startup-project ..\Asn.Diplomski.Server
```

There are no automated tests in this repo yet.

## Architecture

### Clean Architecture Layers

The dependency graph flows inward: `Server → Rdbm → Application → Domain`. The Application layer defines interfaces (`IDeviceRepository`, `ITenantRepository`, `IMqttPublisher`, `IMqttSubscriber`) that are implemented in the outer layers (Rdbm and Server respectively).

### Use Case Pattern

No MediatR. Each use case is a folder under `Application/UseCases/<UseCaseName>/` containing:
- `<Name>Command.cs` — input record
- `<Name>Result.cs` — output (with static factory methods like `Result.Success(...)`, `Result.NotFound()`)
- `<Name>Handler.cs` — the handler class, registered as `AddScoped<Handler>` in `Program.cs`

Controllers inject handlers directly and map between DTOs and commands.

### Domain Model

- **Tenant** owns many **Devices**; each Device owns many **Sensors**
- Device types: `SensorUnit` (auto-provisioned with Temperature + SoilMoisture sensors) and `PumpUnit` (auto-provisioned with WaterLevel + PumpCommand sensors)
- On tenant creation, `CreateTenantHandler` automatically provisions one SensorUnit and one PumpUnit via `CreateDeviceWithSensorsHandler`

### Device Status (`DeviceStatus` enum)

Stored as `int` in the `Status` column of the `devices` table.

| Value | Name | Set by | Condition |
|---|---|---|---|
| 0 | `NotProvisioned` | Initial / migration | Device created but never provisioned |
| 1 | `ProvisioningReady` | `GetProvisioningTokenHandler` | `GET /api/provision` called; token generated |
| 2 | `Provisioning` | `CompleteDeviceProvisioningHandler` | Device exchanged token via `POST /api/provision` |
| 3 | `Ready` | `ConfirmMqttConnectionHandler` | Device confirmed MQTT; subscriptions activated |
| 4 | `Working` | `HandleDeviceStatusHandler` | Received `{"event":"started"}` on MQTT status topic |
| 5 | `Stopped` | `HandleDeviceStatusHandler` | Received `{"event":"stopped"}` on MQTT status topic |

**Commandable devices** (status ∈ {Ready, Working, Stopped}) are targeted by `POST /api/provision/start` and `POST /api/provision/stop`.

### MQTT Pipeline

**Topic formats:**
```
tenant_{tenantId}/device_{deviceId}/sensor_{sensorId}/{slug}   # sensor data
tenant_{tenantId}/device_{deviceId}/command                    # commands to device
tenant_{tenantId}/device_{deviceId}/status                     # lifecycle events from device
```
Sensor slugs: `soil`, `water-level`, `temperature`

**Incoming flow:** `MqttService` (IHostedService) receives messages → writes to `MqttIncomingChannel` (Channel<T>) → `MqttIncomingWorker` (BackgroundService) dispatches based on the last topic segment (slug) via `MqttMessageMapper`:

| Slug | Handler |
|---|---|
| `temperature` | `HandleTemperatureHandler` |
| `soil` | `HandleSoilMoistureHandler` |
| `water-level` | `HandleWaterLevelHandler` |
| `status` | `HandleDeviceStatusHandler` — transitions Working/Stopped |

**Outgoing flow:** Handlers call `IMqttPublisher.Enqueue(tenantId, deviceId, payload)` → builds topic `tenant_{id}/device_{id}/command` → `MqttOutgoingChannel` → `MqttOutgoingWorker` → `MqttService.PublishAsync(...)`.

**Subscription lifecycle:** `POST /api/provision/confirm` → `ConfirmMqttConnectionHandler` calls `IMqttSubscriber.SubscribeToDeviceAsync(device)` → subscribes to all sensor topics **and** the status topic for that device. On broker reconnect, `MqttService.OnConnectedAsync` re-subscribes all previously registered topics automatically.

### Provisioning Endpoints (`ProvisionController`)

| Method | Path | Auth | Handler |
|---|---|---|---|
| GET | `/api/provision` | JWT | `GetProvisioningTokenHandler` |
| POST | `/api/provision` | None | `CompleteDeviceProvisioningHandler` |
| POST | `/api/provision/confirm` | None | `ConfirmMqttConnectionHandler` |
| POST | `/api/provision/start` | JWT | inline — publishes `{"command":"start"}` |
| POST | `/api/provision/stop` | JWT | inline — publishes `{"command":"stop"}` |

See `PROVISIONING.md` (repo root) for the full flow.

### Database

- PostgreSQL via EF Core. Connection string key: `ConnectionStrings:Postgres`
- Dev default: `Host=localhost;Port=5434;Database=asn_db;Username=asn_usr;Password=asn_pass`
- EF migrations live in `Asn.Diplomski.Rdbm/Migrations/`; `AsnDbContext` is the design-time factory target
- Auto-migration runs on startup in `Program.cs`; `DbSeeder` seeds one test tenant if the DB is empty
- `AsnDbContext.SaveChangesAsync` automatically sets `CreatedAt`/`UpdatedAt` timestamps

### Configuration

`appsettings.json` — production defaults (MQTT only):
```json
{ "Mqtt": { "Host": "localhost", "Port": 1883 } }
```

`appsettings.Development.json` — adds the Postgres connection string and MQTT settings.

Swagger UI is served at `/` (root) in Development.

### Automation Logic

`HandleSoilMoistureHandler`: if soil moisture drops below a hardcoded threshold (30%), it enqueues a `pump_on` command to the PumpUnit via MQTT. The threshold is currently a TODO — it should be loaded per-device from the database.
