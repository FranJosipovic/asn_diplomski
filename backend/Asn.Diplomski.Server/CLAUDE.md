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

### MQTT Pipeline

Topic format: `tenant_{tenantId}/device_{deviceId}/sensor/{slug}`  
Slugs: `soil` (soil moisture), `water-level`, `temperature`

**Incoming flow:** `MqttService` (IHostedService) receives messages → writes to `MqttIncomingChannel` (Channel<T>) → `MqttIncomingWorker` (BackgroundService) reads and dispatches to use-case handlers based on the topic slug via `MqttMessageMapper`.

**Outgoing flow:** Use-case handlers call `IMqttPublisher.Enqueue(...)` → `MqttOutgoingChannel` → `MqttOutgoingWorker` → `MqttService.PublishAsync(...)`.

**Device connect flow:** Microcontroller calls `POST /api/devices/{id}/connect` → `ConnectDeviceHandler` → `IMqttSubscriber.SubscribeToDeviceAsync(device)` → MQTT subscription activated. On reconnect, `MqttService.OnConnectedAsync` re-subscribes all active devices automatically.

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
