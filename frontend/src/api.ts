import { SensorType } from './types'
import type { TenantResponse, CreateTenantRequest, AuthState } from './types'

const BASE = '/api'
const AUTH_KEY = 'asn_auth'

// ── Auth storage ──────────────────────────────────────────────

export function getAuth(): AuthState | null {
  try {
    return JSON.parse(localStorage.getItem(AUTH_KEY) ?? 'null')
  } catch {
    return null
  }
}

export function setAuth(auth: AuthState): void {
  localStorage.setItem(AUTH_KEY, JSON.stringify(auth))
}

export function clearAuth(): void {
  localStorage.removeItem(AUTH_KEY)
}

// ── Fetch wrapper with auto-refresh ───────────────────────────

let refreshPromise: Promise<AuthState> | null = null

async function apiFetch(url: string, options: RequestInit = {}, retry = true): Promise<Response> {
  const auth = getAuth()
  const headers = new Headers(options.headers as HeadersInit)
  if (auth) headers.set('Authorization', `Bearer ${auth.accessToken}`)

  const res = await fetch(url, { ...options, headers })

  if (res.status === 401 && retry && auth) {
    // Deduplicate concurrent refresh calls
    if (!refreshPromise) {
      refreshPromise = doRefresh(auth.refreshToken).finally(() => { refreshPromise = null })
    }
    try {
      const newAuth = await refreshPromise
      setAuth(newAuth)
      return apiFetch(url, options, false)
    } catch {
      clearAuth()
      throw new Error('Session expired. Please sign in again.')
    }
  }

  return res
}

async function doRefresh(refreshToken: string): Promise<AuthState> {
  const res = await fetch(`${BASE}/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
  if (!res.ok) throw new Error('Refresh failed')
  const data = await res.json()
  const current = getAuth()!
  return { ...current, accessToken: data.accessToken, refreshToken: data.refreshToken, refreshTokenExpiresAt: data.refreshTokenExpiresAt }
}

async function checkOk(res: Response): Promise<Response> {
  if (res.ok) return res
  let message = `HTTP ${res.status}`
  try {
    const body = await res.json()
    message = body.title ?? body.message ?? JSON.stringify(body)
  } catch {
    message = await res.text() || message
  }
  throw new Error(message)
}

// ── Auth endpoints ────────────────────────────────────────────

export async function signIn(email: string, password: string): Promise<AuthState> {
  const res = await fetch(`${BASE}/auth/signin`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  await checkOk(res)
  const data = await res.json()
  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
    refreshTokenExpiresAt: data.refreshTokenExpiresAt,
    tenantId: data.tenantId,
    email: data.email,
  }
}

// ── Tenant endpoints ──────────────────────────────────────────

export async function createTenant(data: CreateTenantRequest): Promise<TenantResponse> {
  const res = await fetch(`${BASE}/tenants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  await checkOk(res)
  return res.json()
}

export async function getMe(): Promise<TenantResponse> {
  const res = await apiFetch(`${BASE}/tenants/me`)
  if (res.status === 404) throw new Error('Tenant not found')
  await checkOk(res)
  return res.json()
}

// ── Sensor readings ───────────────────────────────────────────

export interface ReadingResponse {
  id: number
  value: number
  recordedAt: string
}

export interface ReadingHistoryResponse {
  sensorId: number
  unit: string
  readings: ReadingResponse[]
}

type ReadingWindow = '1h' | '6h' | '24h' | '7d' | '30d'

export async function getSensorReadings(
  sensorId: number,
  type: SensorType,
  window: ReadingWindow = '24h'
): Promise<ReadingHistoryResponse> {
  const path = type === SensorType.Temperature ? 'temperature' : 'soil-moisture'
  const res = await apiFetch(`${BASE}/sensors/${sensorId}/${path}?window=${window}`)
  if (res.status === 404) throw new Error('No readings available')
  await checkOk(res)
  return res.json()
}
