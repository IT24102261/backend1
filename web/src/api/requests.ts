import { apiClient } from './client'
import type { MediaDto, PagedQuery, PagedResult, RequestDto, RequestHistoryDto, RequestWriteRequest } from '../types/api'

export const requestsApi = {
  list: (query: PagedQuery) =>
    apiClient.get<PagedResult<RequestDto>>('/api/requests', { params: query }).then((r) => r.data),
  get: (id: string) => apiClient.get<RequestDto>(`/api/requests/${id}`).then((r) => r.data),
  create: (payload: RequestWriteRequest) =>
    apiClient.post<RequestDto>('/api/requests', payload).then((r) => r.data),
  update: (id: string, payload: RequestWriteRequest) =>
    apiClient.put<RequestDto>(`/api/requests/${id}`, payload).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/requests/${id}`),
  submit: (id: string) => apiClient.post<RequestDto>(`/api/requests/${id}/submit`).then((r) => r.data),
  history: (id: string) =>
    apiClient.get<RequestHistoryDto[]>(`/api/requests/${id}/history`).then((r) => r.data),
  addClarification: (id: string, message: string) =>
    apiClient.post(`/api/requests/${id}/clarification`, { message }),
  addMedia: (id: string, file: File) => {
    const data = new FormData()
    data.append('file', file)
    return apiClient.post<MediaDto>(`/api/requests/${id}/media`, data).then((r) => r.data)
  },
}
