import { create } from 'zustand'
import { authApi } from '../api/auth'
import { tokenStorage } from '../services/tokenStorage'
import type { LoginRequest, MeResponse, RegisterRequest } from '../types/api'
type AuthState = {
  ready: boolean
  user: MeResponse | null
  accessToken: string | null
  hydrate: () => Promise<void>
  login: (payload: LoginRequest) => Promise<MeResponse>
  register: (payload: RegisterRequest) => Promise<{ user: MeResponse | null; requiresAdminApproval: boolean }>
  logout: () => Promise<void>
  homePath: () => string
}

function pathForRole(role?: string | null) {
  const normalized = String(role ?? '').toUpperCase()
  if (normalized === 'ADMIN') return '/admin/dashboard'
  if (normalized === 'TECHNICIAN') return '/technician/dashboard'
  if (normalized === 'CUSTOMER') return '/customer/dashboard'
  return '/'
}

function postLoginPath(role?: string | null, from?: string | null) {
  const home = pathForRole(role)
  if (!from || from === '/login' || from === '/register' || from === '/unauthorized') {
    return home
  }
  const normalized = String(role ?? '').toUpperCase()
  if (normalized === 'ADMIN' && from.startsWith('/admin')) return from
  if (normalized === 'TECHNICIAN' && from.startsWith('/technician')) return from
  if (normalized === 'CUSTOMER' && from.startsWith('/customer')) return from
  return home
}

export const useAuthStore = create<AuthState>((set, get) => ({
  ready: false,
  user: tokenStorage.getUser(),
  accessToken: tokenStorage.get(),

  async hydrate() {
    const token = tokenStorage.get()
    if (!token) {
      set({ ready: true, user: null, accessToken: null })
      return
    }

    try {
      const user = await authApi.me()
      tokenStorage.setUser(user)
      set({ ready: true, user, accessToken: token })
    } catch {
      tokenStorage.clear()
      set({ ready: true, user: null, accessToken: null })
    }
  },

  async login(payload) {
    const auth = await authApi.login(payload)
    tokenStorage.setSession(auth)
    const user = await authApi.me()
    tokenStorage.setUser(user)
    set({ user, accessToken: auth.accessToken })
    return user
  },

  async register(payload) {
    const auth = await authApi.register(payload)
    if (auth.requiresAdminApproval || !auth.accessToken) {
      return { user: null, requiresAdminApproval: true }
    }
    tokenStorage.setSession(auth)
    const user = await authApi.me()
    tokenStorage.setUser(user)
    set({ user, accessToken: auth.accessToken })
    return { user, requiresAdminApproval: false }
  },

  async logout() {
    const refresh = tokenStorage.getRefresh()
    try {
      if (refresh) await authApi.logout(refresh)
    } catch {
      // Session is cleared locally even if the API is unreachable.
    }
    tokenStorage.clear()
    set({ user: null, accessToken: null })
  },

  homePath: () => pathForRole(get().user?.role),
}))

export { pathForRole, postLoginPath }
