# ASN Diplomski — Dev Commands

## EF Migrations

```bash
dotnet ef database update --project .\Asn.Diplomski.Server\
dotnet ef migrations add StepN_<MIGRATION_NAME> --project Asn.Diplomski.Rdbm --startup-project Asn.Diplomski.Server
```

### Migracija: Guid → long auto-increment ID-evi

```bash
dotnet ef migrations add Step2_LongIds --project Asn.Diplomski.Rdbm --startup-project Asn.Diplomski.Server
dotnet ef database update --project .\Asn.Diplomski.Server\
```

---

## Docker

```bash
# Pokreni sve servise (Postgres + Mosquitto)
docker compose up -d

# Zaustavi sve
docker compose down

# Logovi Mosquitto brokera
docker logs -f asn-mosquitto
```

---

## MQTT — Topic struktura

```
tenant_{tenantId}/device_{deviceId}/sensor/{sensorType}
```

Primjer sa seed tenantom (ID = 1, auto-increment) i stvarnim device ID-em:
```
tenant_{tenantId}/device_{deviceId}/sensor/temperature
tenant_{tenantId}/device_{deviceId}/sensor/soilmoisture
tenant_{tenantId}/device_{deviceId}/sensor/waterlevel
tenant_{tenantId}/device_{deviceId}/sensor/pumpcommand
```

---

## MQTT — Subscribe (slušanje poruka)

### Sve poruke (wildcard)
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 -t "#" -v
```

### Sve poruke jednog tenanta
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/#" -v
```

### Svi senzori jednog uređaja
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/+" -v
```

### Samo temperatura
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/temperature" -v
```

### Samo vlaga tla
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/soilmoisture" -v
```

### Samo razina vode
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/waterlevel" -v
```

### Samo komanda pumpe
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/pumpcommand" -v
```

### Isti tip senzora na svim uređajima tenanta (multi-level wildcard)
```bash
docker exec -it asn-mosquitto mosquitto_sub -h localhost -p 1883 \
  -t "1/+/sensor/temperature" -v
```

---

## MQTT — Publish (slanje testnih poruka)

### Temperatura
```bash
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 \
  -t "tenant_6/device_11/command" \
  -m '{"value": 23.5, "unit": "°C", "timestamp": "2026-05-02T10:00:00Z"}'
```

### Vlaga tla
```bash
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/soilmoisture" \
  -m '{"value": 62.1, "unit": "%", "timestamp": "2026-05-02T10:00:00Z"}'
```

### Razina vode
```bash
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/waterlevel" \
  -m '{"value": 85.0, "unit": "%", "timestamp": "2026-05-02T10:00:00Z"}'
```

### Komanda pumpe
```bash
docker exec -it asn-mosquitto mosquitto_pub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/pumpcommand" \
  -m '{"value": 1, "unit": "bool", "timestamp": "2026-05-02T10:00:00Z"}'
```

---

## MQTT — Ako imaš Mosquitto instaliran lokalno (bez dockera)

```bash
# Subscribe na sve
mosquitto_sub -h localhost -p 1883 -t "#" -v

# Publish temperatura
mosquitto_pub -h localhost -p 1883 \
  -t "1/{deviceId}/sensor/temperature" \
  -m '{"value": 23.5, "unit": "°C"}'
```
