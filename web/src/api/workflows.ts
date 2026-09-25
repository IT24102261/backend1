import { apiClient } from './client'
import type { WorkflowDto, WorkflowStepDto } from '../types/api'

export const workflowsApi = {
  create: (requestId: string, objective: string) =>
    apiClient.post<WorkflowDto>('/api/ai/workflows', { requestId, objective }).then((r) => r.data),
  get: (id: string) => apiClient.get<WorkflowDto>(`/api/ai/workflows/${id}`).then((r) => r.data),
  steps: (id: string) => apiClient.get<WorkflowStepDto[]>(`/api/ai/workflows/${id}/steps`).then((r) => r.data),
  approve: (id: string, reason?: string) =>
    apiClient.post<WorkflowDto>(`/api/ai/workflows/${id}/approve`, { reason }).then((r) => r.data),
  reject: (id: string, reason?: string) =>
    apiClient.post<WorkflowDto>(`/api/ai/workflows/${id}/reject`, { reason }).then((r) => r.data),
  revise: (id: string, reason?: string) =>
    apiClient.post<WorkflowDto>(`/api/ai/workflows/${id}/revise`, { reason }).then((r) => r.data),
}
