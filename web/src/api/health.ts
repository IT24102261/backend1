import { apiClient } from './client'
import type { HealthResponse } from '../types/api'

export async function getHealth() {
  const response = await apiClient.get<HealthResponse>('/health')
  return response.data
}
