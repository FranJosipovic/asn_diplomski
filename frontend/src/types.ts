export enum DeviceType {
  SensorUnit = 1,
  PumpUnit = 2,
}

export enum SensorType {
  Temperature = 'Temperature',
  SoilMoisture = 'SoilMoisture',
  WaterLevel = 'WaterLevel',
  PumpCommand = 'PumpCommand',
}

export interface SensorResponse {
  id: number
  type: SensorType
  sensorNumber: number
  description: string | null
  isActive: boolean
  createdAt: string
}

export type ProvisionStatus = 'NotProvisioned' | 'Provisioning' | 'Provisioned'

export interface DeviceResponse {
  id: number
  type: DeviceType
  provisionStatus: ProvisionStatus
  deviceNumber: number
  description: string | null
  isActive: boolean
  createdAt: string
  lastSeenAt: string | null
  sensors: SensorResponse[]
}

export interface TenantResponse {
  id: number
  name: string
  email: string
  plan: string
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  phoneNumber: string | null
  contactPersonName: string | null
  street: string | null
  city: string | null
  postalCode: string | null
  country: string | null
  devices: DeviceResponse[]
}

export interface CreateTenantRequest {
  name: string
  email: string
  password: string
  plan: string
  phoneNumber?: string
  contactPersonName?: string
  street?: string
  city?: string
  postalCode?: string
  country?: string
}

export interface AuthState {
  accessToken: string
  refreshToken: string
  refreshTokenExpiresAt: string
  tenantId: number
  email: string
}

export interface StoredTenant {
  id: number
  name: string
  email: string
  plan: string
}

export function deviceTypeLabel(type: DeviceType): string {
  switch (type) {
    case DeviceType.SensorUnit: return 'Sensor Unit'
    case DeviceType.PumpUnit: return 'Pump Unit'
  }
}

export function sensorTypeLabel(type: SensorType): string {
  switch (type) {
    case SensorType.Temperature: return 'Temperature'
    case SensorType.SoilMoisture: return 'Soil Moisture'
    case SensorType.WaterLevel: return 'Water Level'
    case SensorType.PumpCommand: return 'Pump Command'
  }
}

export function sensorTypeClass(type: SensorType): string {
  switch (type) {
    case SensorType.Temperature: return 'temperature'
    case SensorType.SoilMoisture: return 'soilmoisture'
    case SensorType.WaterLevel: return 'waterlevel'
    case SensorType.PumpCommand: return 'pumpcommand'
  }
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('hr-HR', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

export function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}

export function isRecentlySeen(iso: string | null): boolean {
  if (!iso) return false
  return Date.now() - new Date(iso).getTime() < 5 * 60 * 1000
}

const STORAGE_KEY = 'asn_recent_tenants'

export function getStoredTenants(): StoredTenant[] {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]')
  } catch {
    return []
  }
}

export function storeTenant(tenant: TenantResponse): void {
  const list = getStoredTenants().filter(t => t.id !== tenant.id)
  list.unshift({ id: tenant.id, name: tenant.name, email: tenant.email, plan: tenant.plan })
  localStorage.setItem(STORAGE_KEY, JSON.stringify(list.slice(0, 10)))
}
