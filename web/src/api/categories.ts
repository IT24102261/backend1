import { apiClient } from './client'
import type { CategoryDto, CategoryWriteRequest } from '../types/api'

export const categoriesApi = {
  list: () => apiClient.get<CategoryDto[]>('/api/categories').then((r) => r.data),
  get: (id: string) => apiClient.get<CategoryDto>(`/api/categories/${id}`).then((r) => r.data),
  create: (payload: CategoryWriteRequest) =>
    apiClient.post<CategoryDto>('/api/categories', payload).then((r) => r.data),
  update: (id: string, payload: CategoryWriteRequest) =>
    apiClient.put<CategoryDto>(`/api/categories/${id}`, payload).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/api/categories/${id}`),
  replaceRequirements: (id: string, requirements: { evidenceType: string; isRequired: boolean; validationMethod: string }[]) =>
    apiClient.put(`/api/categories/${id}/requirements`, requirements).then((r) => r.data),
}
