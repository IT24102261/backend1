import { apiClient } from './client'
import type { AuthResponse, LoginRequest, MeResponse, RegisterRequest } from '../types/api'

export const authApi = {
  login: (payload: LoginRequest) => apiClient.post<AuthResponse>('/api/auth/login', payload).then((r) => r.data),
  register: (payload: RegisterRequest) => {
    if (payload.role !== 'TECHNICIAN') {
      return apiClient.post<AuthResponse>('/api/auth/register', payload).then((r) => r.data)
    }

    const data = new FormData()
    data.append('email', payload.email)
    data.append('password', payload.password)
    data.append('displayName', payload.displayName)
    data.append('role', payload.role)
    if (payload.phone) data.append('phone', payload.phone)
    if (payload.address) data.append('address', payload.address)
    if (payload.categoryId) data.append('categoryId', payload.categoryId)
    if (payload.nicPhoto) data.append('nicPhoto', payload.nicPhoto)
    if (payload.certificate) data.append('certificate', payload.certificate)
    if (payload.profilePhoto) data.append('profilePhoto', payload.profilePhoto)
    return apiClient.post<AuthResponse>('/api/auth/register/technician', data).then((r) => r.data)
  },
  refresh: (refreshToken: string) =>
    apiClient.post<AuthResponse>('/api/auth/refresh', { refreshToken }).then((r) => r.data),
  logout: (refreshToken: string) => apiClient.post('/api/auth/logout', { refreshToken }),
  me: () => apiClient.get<MeResponse>('/api/me').then((r) => r.data),
  updateProfile: (payload: { displayName: string; phone?: string }) =>
    apiClient.put<MeResponse>('/api/me', payload).then((r) => r.data),
  changePassword: (payload: { currentPassword: string; newPassword: string }) =>
    apiClient.post('/api/me/password', payload),
}
