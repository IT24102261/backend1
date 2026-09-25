import type { AuthResponse, MeResponse } from '../types/api'

const ACCESS = 'fixflow.accessToken'
const REFRESH = 'fixflow.refreshToken'
const USER = 'fixflow.user'

export const tokenStorage = {
  get: () => localStorage.getItem(ACCESS),
  getRefresh: () => localStorage.getItem(REFRESH),
  getUser(): MeResponse | null {
    const raw = localStorage.getItem(USER)
    if (!raw) return null
    try {
      return JSON.parse(raw) as MeResponse
    } catch {
      return null
    }
  },
  set(token: string) {
    localStorage.setItem(ACCESS, token)
  },
  setSession(auth: AuthResponse, user?: MeResponse | null) {
    localStorage.setItem(ACCESS, auth.accessToken)
    localStorage.setItem(REFRESH, auth.refreshToken)
    if (user) {
      localStorage.setItem(USER, JSON.stringify(user))
    }
  },
  setUser(user: MeResponse) {
    localStorage.setItem(USER, JSON.stringify(user))
  },
  clear() {
    localStorage.removeItem(ACCESS)
    localStorage.removeItem(REFRESH)
    localStorage.removeItem(USER)
  },
}
