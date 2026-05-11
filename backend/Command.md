# ASN Diplomski — Dev Commands

## EF Migrations

Run from `Asn.Diplomski.Rdbm/`:

```powershell
dotnet ef migrations add Step00N_<Name> --startup-project ..\Asn.Diplomski.Server
dotnet ef database update --startup-project ..\Asn.Diplomski.Server
```

---

## Docker

```powershell
# Start all services (Postgres + Mosquitto)
docker compose up -d

# Stop all
docker compose down

# Mosquitto logs
docker logs -f asn-mosquitto
```

---

## MQTT Topic Structure

```
tenant_{tenantId}/device_{deviceId}/sensor_{sensorId}/{slug}   # sensor data (device → server)
tenant_{tenantId}/device_{deviceId}/command                    # commands    (server → device)
tenant_{tenantId}/device_{deviceId}/status                     # lifecycle   (device → server)
```

Sensor slugs: `temperature`, `soil`, `water-level`

Example with tenant ID 1, device ID 2, sensor IDs 3 and 4:
```
tenant_1/device_2/sensor_3/temperature
tenant_1/device_2/sensor_4/soil
tenant_1/device_2/command
tenant_1/device_2/status
```

---

## MQTT — Subscribe (listen for messages)

### All messages (wildcard)
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "#" -v
```

### All messages for one tenant
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/#" -v
```

### All sensor topics for one device
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/device_2/sensor_+/+" -v
```

### Temperature only
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/device_2/sensor_3/temperature" -v
```

### Soil moisture only
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/device_2/sensor_4/soil" -v
```

### Device status events (started / stopped)
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/device_2/status" -v
```

### Device command topic (see what commands the server sends)
```powershell
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "tenant_1/device_2/command" -v
```

---

## MQTT — Publish (send test messages)

### Simulate temperature reading from device
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/sensor_3/temperature" `
  -m '{"value":23.5,"unit":"°C","timestamp":"2026-05-11T10:00:00Z"}'
```

### Simulate soil moisture reading from device
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/sensor_4/soil" `
  -m '{"value":62,"unit":"%","timestamp":"2026-05-11T10:00:00Z"}'
```

### Simulate device reporting "started" (after receiving start command)
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/status" `
  -m '{"event":"started"}'
```

### Simulate device reporting "stopped" (after receiving stop command)
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/status" `
  -m '{"event":"stopped"}'
```

### Send a start command to a device (bypassing the API — for testing)
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/command" `
  -m '{"command":"start"}'
```

### Send a stop command to a device
```powershell
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 `
  -t "tenant_1/device_2/command" `
  -m '{"command":"stop"}'
```

---

## HTTP — Quick API calls (PowerShell)

### Sign in
```powershell
$resp = Invoke-RestMethod -Uri "http://localhost:5017/api/tenants/signin" `
  -Method POST -ContentType "application/json" `
  -Body '{"email":"test@test.com","password":"password123"}'
$token = $resp.accessToken
```

### Get provisioning data
```powershell
Invoke-RestMethod -Uri "http://localhost:5017/api/provision" `
  -Headers @{ Authorization = "Bearer $token" }
```

### Start system
```powershell
Invoke-RestMethod -Uri "http://localhost:5017/api/provision/start" `
  -Method POST -Headers @{ Authorization = "Bearer $token" }
```

### Stop system
```powershell
Invoke-RestMethod -Uri "http://localhost:5017/api/provision/stop" `
  -Method POST -Headers @{ Authorization = "Bearer $token" }
```
