import { apiClient } from './client'
import type {
  BookingDto,
  BookingHistoryDto,
  InvitationDto,
  PagedQuery,
  PagedResult,
  QuoteDto,
  QuoteWriteRequest,
  ScopeChangeDto,
} from '../types/api'

export const marketplaceApi = {
  invitations: () => apiClient.get<InvitationDto[]>('/api/invitations').then((r) => r.data),
  invitation: (id: string) => apiClient.get<InvitationDto>(`/api/invitations/${id}`).then((r) => r.data),
  accept: (id: string) => apiClient.post<InvitationDto>(`/api/invitations/${id}/accept`).then((r) => r.data),
  decline: (id: string) => apiClient.post<InvitationDto>(`/api/invitations/${id}/decline`).then((r) => r.data),
  createQuote: (invitationId: string, payload: QuoteWriteRequest) =>
    apiClient.post<QuoteDto>(`/api/invitations/${invitationId}/quote`, payload).then((r) => r.data),
  requestQuotes: (requestId: string) =>
    apiClient.get<QuoteDto[]>(`/api/requests/${requestId}/quotes`).then((r) => r.data),
  quote: (id: string) => apiClient.get<QuoteDto>(`/api/quotes/${id}`).then((r) => r.data),
  withdrawQuote: (id: string) => apiClient.post<QuoteDto>(`/api/quotes/${id}/withdraw`).then((r) => r.data),
  selectQuote: (id: string) => apiClient.post<BookingDto>(`/api/quotes/${id}/select`).then((r) => r.data),
  confirmBooking: (bookingId: string) =>
    apiClient.post<BookingDto>('/api/bookings/confirm', { bookingId }).then((r) => r.data),
  bookings: (query: PagedQuery) =>
    apiClient.get<PagedResult<BookingDto>>('/api/bookings', { params: query }).then((r) => r.data),
  booking: (id: string) => apiClient.get<BookingDto>(`/api/bookings/${id}`).then((r) => r.data),
  updateBookingStatus: (id: string, status: string, note?: string) =>
    apiClient.patch<BookingDto>(`/api/bookings/${id}/status`, { status, note }).then((r) => r.data),
  bookingHistory: (id: string) =>
    apiClient.get<BookingHistoryDto[]>(`/api/bookings/${id}/history`).then((r) => r.data),
  scopeChanges: (bookingId: string) =>
    apiClient.get<ScopeChangeDto[]>(`/api/bookings/${bookingId}/scope-changes`).then((r) => r.data),
  proposeScopeChange: (bookingId: string, payload: { description: string; proposedCost: number }) =>
    apiClient.post<ScopeChangeDto>(`/api/bookings/${bookingId}/scope-changes`, payload).then((r) => r.data),
  decideScopeChange: (id: string, decision: string) =>
    apiClient.post<ScopeChangeDto>(`/api/bookings/scope-changes/${id}/decision`, { decision }).then((r) => r.data),
}
