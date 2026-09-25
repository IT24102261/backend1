import { apiClient } from './client'
import type {
  AdminCreateUserRequest,
  AdminUserDto,
  AgentReportDto,
  AuditLogDto,
  BookingReportDto,
  DashboardDto,
  PagedQuery,
  PagedResult,
  RequestReportDto,
  TechnicianReportDto,
} from '../types/api'

export const adminApi = {
  dashboard: () => apiClient.get<DashboardDto>('/api/admin/dashboard').then((r) => r.data),
  requestReports: (query: PagedQuery) =>
    apiClient.get<PagedResult<RequestReportDto>>('/api/admin/reports/requests', { params: query }).then((r) => r.data),
  bookingReports: (query: PagedQuery) =>
    apiClient.get<PagedResult<BookingReportDto>>('/api/admin/reports/bookings', { params: query }).then((r) => r.data),
  technicianReports: (query: PagedQuery) =>
    apiClient
      .get<PagedResult<TechnicianReportDto>>('/api/admin/reports/technicians', { params: query })
      .then((r) => r.data),
  agentReports: (query: PagedQuery) =>
    apiClient.get<PagedResult<AgentReportDto>>('/api/admin/reports/agents', { params: query }).then((r) => r.data),
  audit: (query: PagedQuery) =>
    apiClient.get<PagedResult<AuditLogDto>>('/api/admin/audit', { params: query }).then((r) => r.data),
  users: (query: PagedQuery) =>
    apiClient.get<PagedResult<AdminUserDto>>('/api/admin/users', { params: query }).then((r) => r.data),
  createUser: (payload: AdminCreateUserRequest) =>
    apiClient.post<AdminUserDto>('/api/admin/users', payload).then((r) => r.data),
  setUserActive: (id: string, isActive: boolean) =>
    apiClient.patch<AdminUserDto>(`/api/admin/users/${id}/status`, { isActive }).then((r) => r.data),
}
