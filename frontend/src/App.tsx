import { useState, useEffect } from 'react'
import HomeView from './views/HomeView'
import TenantView from './views/TenantView'
import { getStoredTenants, storeTenant } from './types'
import type { TenantResponse, StoredTenant } from './types'

type View = { kind: 'home' } | { kind: 'tenant'; id: number }

function Clock() {
  const [time, setTime] = useState(new Date())
  useEffect(() => {
    const t = setInterval(() => setTime(new Date()), 1000)
    return () => clearInterval(t)
  }, [])
  return (
    <span className="header-time">
      {time.toLocaleTimeString('hr-HR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
    </span>
  )
}

export default function App() {
  const [view, setView] = useState<View>({ kind: 'home' })
  const [recentTenants, setRecentTenants] = useState<StoredTenant[]>(() => getStoredTenants())

  function openTenant(id: number) {
    setView({ kind: 'tenant', id })
  }

  function handleTenantCreated(tenant: TenantResponse) {
    storeTenant(tenant)
    setRecentTenants(getStoredTenants())
  }

  return (
    <>
      <header className="header">
        <div className="header-logo">
          <div className="header-mark">
            <div className="header-mark-inner" />
          </div>
          <div className="header-wordmark">
            <div className="header-title">
              ASN<span> Control</span>
            </div>
            <div className="header-sub">Automated Irrigation System</div>
          </div>
        </div>

        <div className="header-right">
          <Clock />
          <div className="sys-status">
            <div className="pulse-dot" />
            System Online
          </div>
        </div>
      </header>

      <main className="main">
        {view.kind === 'home' ? (
          <HomeView
            recentTenants={recentTenants}
            onOpenTenant={id => openTenant(id)}
            onTenantCreated={handleTenantCreated}
          />
        ) : (
          <TenantView
            id={view.id}
            onBack={() => setView({ kind: 'home' })}
          />
        )}
      </main>
    </>
  )
}
