import { useEffect, useState, useCallback } from 'react'
import { getTenant } from '../api'
import type { TenantResponse } from '../types'
import { formatDateTime } from '../types'
import DeviceCard from '../components/DeviceCard'

interface Props {
  id: number
  onBack: () => void
}

export default function TenantView({ id, onBack }: Props) {
  const [tenant, setTenant] = useState<TenantResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshing, setRefreshing] = useState(false)

  const load = useCallback(async (silent = false) => {
    if (!silent) setLoading(true)
    else setRefreshing(true)
    setError(null)
    try {
      const data = await getTenant(id)
      setTenant(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tenant')
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }, [id])

  useEffect(() => { load() }, [load])

  // Auto-refresh every 30s
  useEffect(() => {
    const timer = setInterval(() => load(true), 30_000)
    return () => clearInterval(timer)
  }, [load])

  if (loading) {
    return (
      <div>
        <button className="back-btn" onClick={onBack}>← Back</button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, color: 'var(--text-1)', fontFamily: 'var(--font-mono)', fontSize: 12 }}>
          <div className="spinner" /> Loading tenant {id}…
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div>
        <button className="back-btn" onClick={onBack}>← Back</button>
        <div className="msg msg-error">{error}</div>
      </div>
    )
  }

  if (!tenant) return null

  const nil = (v: string | null) =>
    v ? <span className="info-val">{v}</span> : <span className="info-val nil">—</span>

  return (
    <div>
      <button className="back-btn" onClick={onBack}>← All Tenants</button>

      {/* Header */}
      <div className="tenant-header">
        <div>
          <h1 className="tenant-name">{tenant.name}</h1>
          <p className="tenant-email">{tenant.email}</p>
          <div className="tenant-badges">
            <span className={`badge ${tenant.isActive ? 'badge-active' : 'badge-offline'}`}>
              {tenant.isActive ? 'Active' : 'Inactive'}
            </span>
            <span className="badge badge-plan">{tenant.plan}</span>
          </div>
        </div>

        <div className="tenant-actions">
          <button
            className="btn btn-ghost btn-sm"
            onClick={() => load(true)}
            disabled={refreshing}
          >
            {refreshing ? <><div className="spinner" /> Refreshing</> : '↻ Refresh'}
          </button>
          <div style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-2)', textAlign: 'right', letterSpacing: 1 }}>
            AUTO·REFRESH<br />30s
          </div>
        </div>
      </div>

      {/* Info grid */}
      <div className="info-grid">
        <div className="info-item">
          <div className="info-key">Tenant ID</div>
          <div className="info-val">{tenant.id}</div>
        </div>
        <div className="info-item">
          <div className="info-key">Created</div>
          <div className="info-val">{formatDateTime(tenant.createdAt)}</div>
        </div>
        <div className="info-item">
          <div className="info-key">Updated</div>
          {tenant.updatedAt ? <div className="info-val">{formatDateTime(tenant.updatedAt)}</div> : <div className="info-val nil">—</div>}
        </div>
        <div className="info-item">
          <div className="info-key">Contact Person</div>
          {nil(tenant.contactPersonName)}
        </div>
        <div className="info-item">
          <div className="info-key">Phone</div>
          {nil(tenant.phoneNumber)}
        </div>
        <div className="info-item">
          <div className="info-key">City</div>
          {nil(tenant.city)}
        </div>
        <div className="info-item">
          <div className="info-key">Country</div>
          {nil(tenant.country)}
        </div>
        <div className="info-item">
          <div className="info-key">Devices</div>
          <div className="info-val">{tenant.devices.length}</div>
        </div>
      </div>

      {/* Devices */}
      <div className="section-head">
        <span className="section-label">Devices</span>
        <div className="section-rule" />
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 10, color: 'var(--text-2)', whiteSpace: 'nowrap' }}>
          {tenant.devices.filter(d => d.isActive).length} active / {tenant.devices.length} total
        </span>
      </div>

      {tenant.devices.length === 0 ? (
        <div className="empty">No devices provisioned</div>
      ) : (
        <div className="devices-grid">
          {tenant.devices.map(device => (
            <DeviceCard key={device.id} device={device} />
          ))}
        </div>
      )}
    </div>
  )
}
