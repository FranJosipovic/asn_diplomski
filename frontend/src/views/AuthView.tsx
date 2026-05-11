import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { signIn as apiSignIn, createTenant } from '../api'
import { useAuth } from '../context/AuthContext'
import CreateTenantForm from '../components/CreateTenantForm'
import type { TenantResponse } from '../types'

interface Props {
  defaultTab: 'signin' | 'register'
}

export default function AuthView({ defaultTab }: Props) {
  const { signIn } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSignIn(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const auth = await apiSignIn(email, password)
      signIn(auth)
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign in failed')
    } finally {
      setLoading(false)
    }
  }

  async function handleRegistered(tenant: TenantResponse, credentials: { email: string; password: string }) {
    try {
      const auth = await apiSignIn(credentials.email, credentials.password)
      signIn(auth)
      navigate('/dashboard', { replace: true })
    } catch {
      navigate('/signin')
    }
  }

  return (
    <div className="auth-layout">
      <div className="auth-brand">
        <p className="page-eyebrow">ASN Control v0.1</p>
        <h1 className="page-title">Irrigation<br />Management</h1>
        <p className="auth-tagline">
          Monitor your sensors, manage devices,<br />and control irrigation from one place.
        </p>
      </div>

      <div className="auth-card">
        <div className="auth-tabs">
          <Link
            to="/signin"
            className={`auth-tab ${defaultTab === 'signin' ? 'active' : ''}`}
            onClick={() => setError(null)}
          >
            Sign In
          </Link>
          <Link
            to="/register"
            className={`auth-tab ${defaultTab === 'register' ? 'active' : ''}`}
            onClick={() => setError(null)}
          >
            Create Account
          </Link>
        </div>

        {defaultTab === 'signin' ? (
          <div className="auth-form">
            {error && <div className="msg msg-error" style={{ marginBottom: 16 }}>{error}</div>}
            <form onSubmit={handleSignIn}>
              <div className="stack-md">
                <div className="field">
                  <label className="field-label">Email <span className="req">*</span></label>
                  <input
                    className="field-input"
                    type="email"
                    placeholder="your@email.com"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    required
                    autoComplete="email"
                  />
                </div>
                <div className="field">
                  <label className="field-label">Password <span className="req">*</span></label>
                  <input
                    className="field-input"
                    type="password"
                    placeholder="••••••••"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    required
                    autoComplete="current-password"
                  />
                </div>
                <button className="btn btn-primary" type="submit" disabled={loading} style={{ width: '100%' }}>
                  {loading ? <><div className="spinner" /> Signing in…</> : 'Sign In'}
                </button>
              </div>
            </form>
          </div>
        ) : (
          <div className="auth-form">
            {error && <div className="msg msg-error" style={{ marginBottom: 16 }}>{error}</div>}
            <CreateTenantForm onSuccess={handleRegistered} />
          </div>
        )}
      </div>
    </div>
  )
}
