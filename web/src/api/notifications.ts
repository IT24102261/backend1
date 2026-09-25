import { apiClient } from './client'
import type { NotificationDto } from '../types/api'

export const notificationsApi = {
  list: () => apiClient.get<NotificationDto[]>('/api/notifications').then((r) => r.data),
  markRead: (id: string) => apiClient.post(`/api/notifications/${id}/read`),
}
