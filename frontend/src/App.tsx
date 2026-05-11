import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import AuthView from './views/AuthView'
import TenantView from './views/TenantView'
import SensorDetailPage from './views/SensorDetailPage'

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

function GuestRoute() {
  const { auth } = useAuth()
  return auth ? <Navigate to="/dashboard" replace /> : <Outlet />
}

function ProtectedRoute() {
  const { auth } = useAuth()
  return auth ? <Outlet /> : <Navigate to="/signin" replace />
}

function AppShell() {
  const { auth } = useAuth()
  return (
    <>
      <header className="header">
        <div className="header-logo">
          <div className="header-mark">
            <div className="header-mark-inner" />
          </div>
          <div className="header-wordmark">
            <div className="header-title">ASN<span> Control</span></div>
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
        <Routes>
          <Route element={<GuestRoute />}>
            <Route path="/signin" element={<AuthView defaultTab="signin" />} />
            <Route path="/register" element={<AuthView defaultTab="register" />} />
          </Route>
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<TenantView />} />
            <Route path="/dashboard/sensor/:sensorId" element={<SensorDetailPage />} />
          </Route>
          <Route path="*" element={<Navigate to={auth ? '/dashboard' : '/signin'} replace />} />
        </Routes>
      </main>
    </>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppShell />
      </AuthProvider>
    </BrowserRouter>
  )
}
