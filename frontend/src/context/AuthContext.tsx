import { createContext, useContext, useState } from 'react'
import { getAuth, setAuth as persistAuth, clearAuth } from '../api'
import type { AuthState } from '../types'

interface AuthContextValue {
  auth: AuthState | null
  signIn: (auth: AuthState) => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(() => getAuth())

  function signIn(newAuth: AuthState) {
    persistAuth(newAuth)
    setAuth(newAuth)
  }

  function signOut() {
    clearAuth()
    setAuth(null)
  }

  return (
    <AuthContext.Provider value={{ auth, signIn, signOut }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
