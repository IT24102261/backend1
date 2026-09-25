import { apiClient } from './client'
import type { PagedQuery, PagedResult, ReviewDto } from '../types/api'

export const reviewsApi = {
  create: (bookingId: string, rating: number, body?: string) =>
    apiClient.post<ReviewDto>(`/api/bookings/${bookingId}/reviews`, { rating, body }).then((r) => r.data),
  mine: () => apiClient.get<ReviewDto[]>('/api/reviews/mine').then((r) => r.data),
  forTechnician: (technicianId: string) =>
    apiClient.get<ReviewDto[]>(`/api/technicians/${technicianId}/reviews`).then((r) => r.data),
  adminList: (query: PagedQuery) =>
    apiClient.get<PagedResult<ReviewDto>>('/api/admin/reviews', { params: query }).then((r) => r.data),
  moderate: (id: string, status: string, reason?: string) =>
    apiClient.patch<ReviewDto>(`/api/admin/reviews/${id}/moderate`, { status, reason }).then((r) => r.data),
  update: (id: string, rating: number, body?: string) =>
    apiClient.patch<ReviewDto>(`/api/admin/reviews/${id}`, { rating, body }).then((r) => r.data),
  reply: (id: string, reply: string) =>
    apiClient.post<ReviewDto>(`/api/admin/reviews/${id}/reply`, { reply }).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/admin/reviews/${id}`),
}
