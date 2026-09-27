import { apiClient } from './client'
import type {
  DocumentDto,
  PagedQuery,
  PagedResult,
  ReviewDto,
  TechnicianApplicationDto,
  TechnicianProfileDto,
  TechnicianProfileRequest,
  PublicTechnicianDto,
} from '../types/api'

export const techniciansApi = {
  getProfile: () => apiClient.get<TechnicianProfileDto>('/api/technicians/profile').then((r) => r.data),
  createProfile: (payload: TechnicianProfileRequest) =>
    apiClient.post<TechnicianProfileDto>('/api/technicians/profile', payload).then((r) => r.data),
  updateProfile: (payload: TechnicianProfileRequest) =>
    apiClient.put<TechnicianProfileDto>('/api/technicians/profile', payload).then((r) => r.data),
  reviews: (technicianId: string) =>
    apiClient.get<ReviewDto[]>(`/api/technicians/${technicianId}/reviews`).then((r) => r.data),
  publicProfile: (technicianId: string) =>
    apiClient.get<PublicTechnicianDto>(`/api/technicians/${technicianId}/public`).then((r) => r.data),
  apply: (categoryId: string) =>
    apiClient.post<TechnicianApplicationDto>('/api/technician-applications', { categoryId }).then((r) => r.data),
  myApplications: () =>
    apiClient.get<TechnicianApplicationDto[]>('/api/technician-applications').then((r) => r.data),
  getApplication: (id: string) =>
    apiClient.get<TechnicianApplicationDto>(`/api/technician-applications/${id}`).then((r) => r.data),
  uploadDocument: (id: string, file: File, evidenceType: string) => {
    const data = new FormData()
    data.append('file', file)
    data.append('evidenceType', evidenceType)
    return apiClient.post<DocumentDto>(`/api/technician-applications/${id}/documents`, data).then((r) => r.data)
  },
  adminApplications: (query: PagedQuery) =>
    apiClient
      .get<PagedResult<TechnicianApplicationDto>>('/api/admin/technician-applications', { params: query })
      .then((r) => r.data),
  adminApplication: (id: string) =>
    apiClient.get<TechnicianApplicationDto>(`/api/admin/technician-applications/${id}`).then((r) => r.data),
  decide: (id: string, action: 'approve' | 'reject' | 'request-info' | 'reverify' | 'suspend', notes?: string) =>
    apiClient
      .post<TechnicianApplicationDto>(`/api/admin/technician-applications/${id}/${action}`, { notes })
      .then((r) => r.data),
  suspend: (id: string, notes?: string) =>
    apiClient.post<TechnicianProfileDto>(`/api/admin/technicians/${id}/suspend`, { notes }).then((r) => r.data),
  reactivate: (id: string, notes?: string) =>
    apiClient.post<TechnicianProfileDto>(`/api/admin/technicians/${id}/reactivate`, { notes }).then((r) => r.data),
  setProfilePhoto: (id: string, file: File) => {
    const data = new FormData()
    data.append('file', file)
    return apiClient.post<TechnicianProfileDto>(`/api/admin/technicians/${id}/photo`, data).then((r) => r.data)
  },
}
