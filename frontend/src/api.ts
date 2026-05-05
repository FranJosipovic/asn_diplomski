import { TenantResponse, CreateTenantRequest, SensorType } from "./types";

const BASE = "/api";

export async function createTenant(
  data: CreateTenantRequest,
): Promise<TenantResponse> {
  const res = await fetch(`${BASE}/tenants`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!res.ok) {
    let message = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      message = body.title ?? body.message ?? JSON.stringify(body);
    } catch {
      message = await res.text();
    }
    throw new Error(message);
  }

  return res.json();
}

export async function getTenant(id: number): Promise<TenantResponse> {
  const res = await fetch(`${BASE}/tenants/${id}`);

  if (!res.ok) {
    if (res.status === 404) throw new Error("Tenant not found");
    throw new Error(`HTTP ${res.status}`);
  }

  return res.json();
}

export interface ReadingResponse {
  id: number;
  value: number;
  recordedAt: string;
}

export interface ReadingHistoryResponse {
  sensorId: number;
  unit: string;
  readings: ReadingResponse[];
}

export async function getSensorReadings(
  sensorId: number,
  sensorType: SensorType,
  window: "1h" | "6h" | "24h" | "7d" | "30d" = "24h",
): Promise<ReadingHistoryResponse> {
  let sensorSlug;
  if (sensorType === SensorType.Temperature) {
    sensorSlug = "temperature";
  } else if (sensorType === SensorType.SoilMoisture) {
    sensorSlug = "soil-moisture";
  }

  const res = await fetch(
    `${BASE}/sensors/${sensorId}/${sensorSlug}?window=${window}`,
  );

  if (!res.ok) {
    if (res.status === 404) throw new Error("No readings available");
    throw new Error(`HTTP ${res.status}`);
  }

  return res.json();
}
