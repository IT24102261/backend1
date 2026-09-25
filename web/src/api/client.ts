import axios, { type InternalAxiosRequestConfig } from 'axios'
import { tokenStorage } from '../services/tokenStorage'
import type { AuthResponse } from '../types/api'

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080'

export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
})

const refreshClient = axios.create({ baseURL })

let refreshPromise: Promise<string | null> | null = null

function applyAuth(config: InternalAxiosRequestConfig) {
  const token = tokenStorage.get()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  if (config.data instanceof FormData) {
    delete config.headers['Content-Type']
  }
  return config
}

async function refreshAccessToken() {
  const refreshToken = tokenStorage.getRefresh()
  if (!refreshToken) return null

  const response = await refreshClient.post<AuthResponse>('/api/auth/refresh', { refreshToken })
  const existing = tokenStorage.getUser()
  tokenStorage.setSession(response.data, existing)
  return response.data.accessToken
}

apiClient.interceptors.request.use(applyAuth)

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
    if (error.response?.status !== 401 || original._retry || original.url?.includes('/api/auth/')) {
      return Promise.reject(error)
    }

    original._retry = true
    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null
    })

    const nextToken = await refreshPromise
    if (!nextToken) {
      tokenStorage.clear()
      return Promise.reject(error)
    }

    original.headers.Authorization = `Bearer ${nextToken}`
    return apiClient(original)
  },
)
