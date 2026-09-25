import { apiClient } from './client'
import type { ComplaintDto, PagedQuery, PagedResult } from '../types/api'

export const complaintsApi = {
  create: (payload: { bookingId?: string; subject: string; description: string }) =>
    apiClient.post<ComplaintDto>('/api/complaints', payload).then((r) => r.data),
  mine: (query: PagedQuery) =>
    apiClient.get<PagedResult<ComplaintDto>>('/api/complaints', { params: query }).then((r) => r.data),
  adminList: (query: PagedQuery) =>
    apiClient.get<PagedResult<ComplaintDto>>('/api/admin/complaints', { params: query }).then((r) => r.data),
  updateStatus: (id: string, status: string, resolution?: string) =>
    apiClient.patch<ComplaintDto>(`/api/admin/complaints/${id}/status`, { status, resolution }).then((r) => r.data),
  reply: (id: string, reply: string) =>
    apiClient.post<ComplaintDto>(`/api/admin/complaints/${id}/reply`, { reply }).then((r) => r.data),
}
