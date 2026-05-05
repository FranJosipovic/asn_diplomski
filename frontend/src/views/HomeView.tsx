import { useState } from 'react'
import { getStoredTenants } from '../types'
import type { TenantResponse, StoredTenant } from '../types'
import CreateTenantForm from '../components/CreateTenantForm'
import { getTenant } from '../api'

interface Props {
  onOpenTenant: (id: number) => void
  onTenantCreated: (tenant: TenantResponse) => void
  recentTenants: StoredTenant[]
}

export default function HomeView({ onOpenTenant, onTenantCreated, recentTenants }: Props) {
  const [findId, setFindId] = useState('')
  const [findError, setFindError] = useState<string | null>(null)
  const [findLoading, setFindLoading] = useState(false)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  function handleFormSuccess(tenant: TenantResponse) {
    onTenantCreated(tenant)
    setSuccessMsg(`Tenant "${tenant.name}" created — ID ${tenant.id}. Auto-provisioned ${tenant.devices.length} device(s).`)
    setTimeout(() => onOpenTenant(tenant.id), 1200)
  }

  async function handleFind(e: React.FormEvent) {
    e.preventDefault()
    const id = parseInt(findId, 10)
    if (!id || isNaN(id)) {
      setFindError('Enter a valid numeric ID')
      return
    }
    setFindLoading(true)
    setFindError(null)
    try {
      await getTenant(id)
      onOpenTenant(id)
    } catch (err) {
      setFindError(err instanceof Error ? err.message : 'Not found')
    } finally {
      setFindLoading(false)
    }
  }

  return (
    <div>
      <p className="page-eyebrow">ASN Control v0.1</p>
      <h1 className="page-title">Irrigation<br />Management</h1>

      <div className="home-grid">
        {/* ── Left: Create form ── */}
        <div>
          <div className="section-head">
            <span className="section-label">New Tenant</span>
            <div className="section-rule" />
          </div>

          <div className="panel">
            <div className="panel-head">
              <span className="panel-title">Provision tenant + devices</span>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 10, color: 'var(--text-2)' }}>
                Auto-provisions 1× SensorUnit + 1× PumpUnit
              </span>
            </div>
            <div className="panel-body">
              {successMsg && (
                <div className="msg msg-success" style={{ marginBottom: 16 }}>
                  {successMsg}
                </div>
              )}
              <CreateTenantForm onSuccess={handleFormSuccess} />
            </div>
          </div>
        </div>

        {/* ── Right: Recent + Find ── */}
        <div>
          <div className="section-head">
            <span className="section-label">Tenants</span>
            <div className="section-rule" />
          </div>

          {/* Find by ID */}
          <div className="panel" style={{ marginBottom: 16 }}>
            <div className="panel-head">
              <span className="panel-title">Find by ID</span>
            </div>
            <div className="panel-body">
              <form onSubmit={handleFind}>
                <div className="find-row">
                  <input
                    className="field-input"
                    placeholder="Tenant ID…"
                    value={findId}
                    onChange={e => { setFindId(e.target.value); setFindError(null) }}
                    style={{ flexShrink: 1 }}
                  />
                  <button className="btn btn-ghost btn-sm" type="submit" disabled={findLoading}>
                    {findLoading ? <div className="spinner" /> : 'Open'}
                  </button>
                </div>
                {findError && (
                  <div className="msg msg-error" style={{ marginTop: 10 }}>
                    {findError}
                  </div>
                )}
              </form>
            </div>
          </div>

          {/* Recent */}
          <div className="panel">
            <div className="panel-head">
              <span className="panel-title">Recent</span>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 10, color: 'var(--text-2)' }}>
                {recentTenants.length} stored
              </span>
            </div>
            <div className="panel-body" style={{ padding: '12px 16px' }}>
              {recentTenants.length === 0 ? (
                <div className="empty">No tenants yet</div>
              ) : (
                <div className="stack-sm">
                  {recentTenants.map(t => (
                    <div
                      key={t.id}
                      className="tenant-item"
                      onClick={() => onOpenTenant(t.id)}
                    >
                      <div>
                        <div className="tenant-item-name">{t.name}</div>
                        <div className="tenant-item-meta">{t.email} · {t.plan}</div>
                      </div>
                      <span className="tenant-item-arrow">→</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
