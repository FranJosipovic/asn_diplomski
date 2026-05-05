import { useState } from 'react'
import { createTenant } from '../api'
import type { TenantResponse } from '../types'

interface Props {
  onSuccess: (tenant: TenantResponse) => void
}

interface FormState {
  name: string
  email: string
  password: string
  plan: string
  phoneNumber: string
  contactPersonName: string
  street: string
  city: string
  postalCode: string
  country: string
}

const empty: FormState = {
  name: '', email: '', password: '', plan: 'basic',
  phoneNumber: '', contactPersonName: '',
  street: '', city: '', postalCode: '', country: '',
}

export default function CreateTenantForm({ onSuccess }: Props) {
  const [form, setForm] = useState<FormState>(empty)
  const [showOptional, setShowOptional] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function set(field: keyof FormState) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
      setForm(f => ({ ...f, [field]: e.target.value }))
      setError(null)
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const tenant = await createTenant({
        name: form.name,
        email: form.email,
        password: form.password,
        plan: form.plan,
        ...(form.phoneNumber        && { phoneNumber: form.phoneNumber }),
        ...(form.contactPersonName  && { contactPersonName: form.contactPersonName }),
        ...(form.street             && { street: form.street }),
        ...(form.city               && { city: form.city }),
        ...(form.postalCode         && { postalCode: form.postalCode }),
        ...(form.country            && { country: form.country }),
      })
      setForm(empty)
      onSuccess(tenant)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      {/* Required fields */}
      <div className="form-grid single stack-sm">
        <div className="field">
          <label className="field-label">Organization Name <span className="req">*</span></label>
          <input
            className="field-input"
            placeholder="Farma Josipović d.o.o."
            value={form.name}
            onChange={set('name')}
            required
          />
        </div>
      </div>

      <div className="form-grid mt-12" style={{ marginTop: 14 }}>
        <div className="field">
          <label className="field-label">Email <span className="req">*</span></label>
          <input
            className="field-input"
            type="email"
            placeholder="admin@farma.hr"
            value={form.email}
            onChange={set('email')}
            required
          />
        </div>
        <div className="field">
          <label className="field-label">Password <span className="req">*</span></label>
          <input
            className="field-input"
            type="password"
            placeholder="Min. 8 characters"
            value={form.password}
            onChange={set('password')}
            minLength={8}
            required
          />
        </div>
      </div>

      <div className="field" style={{ marginTop: 14 }}>
        <label className="field-label">Plan</label>
        <select className="field-select" value={form.plan} onChange={set('plan')}>
          <option value="basic">Basic</option>
          <option value="pro">Pro</option>
          <option value="enterprise">Enterprise</option>
        </select>
      </div>

      {/* Optional section */}
      <button
        type="button"
        className="optional-toggle"
        onClick={() => setShowOptional(v => !v)}
        style={{ marginTop: 16 }}
      >
        <span className={`toggle-arrow ${showOptional ? 'open' : ''}`}>▶</span>
        Optional contact &amp; address
      </button>

      {showOptional && (
        <>
          <div className="form-grid" style={{ marginBottom: 14 }}>
            <div className="field">
              <label className="field-label">Contact Person</label>
              <input
                className="field-input"
                placeholder="Ivan Horvat"
                value={form.contactPersonName}
                onChange={set('contactPersonName')}
              />
            </div>
            <div className="field">
              <label className="field-label">Phone Number</label>
              <input
                className="field-input"
                placeholder="+385 91 123 4567"
                value={form.phoneNumber}
                onChange={set('phoneNumber')}
              />
            </div>
          </div>

          <div className="field" style={{ marginBottom: 14 }}>
            <label className="field-label">Street</label>
            <input
              className="field-input"
              placeholder="Ilica 1"
              value={form.street}
              onChange={set('street')}
            />
          </div>

          <div className="form-grid triple">
            <div className="field">
              <label className="field-label">City</label>
              <input
                className="field-input"
                placeholder="Zagreb"
                value={form.city}
                onChange={set('city')}
              />
            </div>
            <div className="field">
              <label className="field-label">Postal Code</label>
              <input
                className="field-input"
                placeholder="10000"
                value={form.postalCode}
                onChange={set('postalCode')}
              />
            </div>
            <div className="field">
              <label className="field-label">Country</label>
              <input
                className="field-input"
                placeholder="Croatia"
                value={form.country}
                onChange={set('country')}
              />
            </div>
          </div>
        </>
      )}

      {error && (
        <div className="msg msg-error" style={{ marginTop: 16 }}>
          {error}
        </div>
      )}

      <div style={{ marginTop: 20 }}>
        <button className="btn btn-primary" type="submit" disabled={loading}>
          {loading ? <><div className="spinner" /> Provisioning…</> : 'Create Tenant'}
        </button>
      </div>
    </form>
  )
}
