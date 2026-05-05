import type { TenantResponse, CreateTenantRequest } from './types'

const BASE = '/api'

export async function createTenant(data: CreateTenantRequest): Promise<TenantResponse> {
  const res = await fetch(`${BASE}/tenants`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const body = await res.json()
      message = body.title ?? body.message ?? JSON.stringify(body)
    } catch {
      message = await res.text()
    }
    throw new Error(message)
  }

  return res.json()
}

export async function getTenant(id: number): Promise<TenantResponse> {
  const res = await fetch(`${BASE}/tenants/${id}`)

  if (!res.ok) {
    if (res.status === 404) throw new Error('Tenant not found')
    throw new Error(`HTTP ${res.status}`)
  }

  return res.json()
}
