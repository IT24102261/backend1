export type Role = 'CUSTOMER' | 'TECHNICIAN' | 'ADMIN'

export type HealthResponse = {
  status: string
  service: string
}

export type ApiError = {
  error: string
  code: string
  details?: string[]
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type PagedQuery = {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDir?: 'asc' | 'desc'
  status?: string
  role?: string
}

export type AuthResponse = {
  userId: string
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
  email: string
  role: Role | string
  requiresAdminApproval?: boolean
}

export type MeResponse = {
  userId: string
  email: string
  displayName: string
  phone?: string | null
  role: Role | string
  isActive: boolean
  technicianProfileId?: string | null
  approvedCategories: string[]
}

export type RegisterRequest = {
  email: string
  password: string
  displayName: string
  phone?: string
  address?: string
  categoryId?: string
  role: 'CUSTOMER' | 'TECHNICIAN'
  nicPhoto?: File
  certificate?: File
  profilePhoto?: File
}

export type LoginRequest = {
  email: string
  password: string
}

export type CategoryDto = {
  id: string
  name: string
  description?: string | null
  parentId?: string | null
  isActive: boolean
  requirements?: { id: string; evidenceType: string; isRequired: boolean; validationMethod: string; ruleVersion: string }[]
}

export type CategoryWriteRequest = {
  name: string
  description?: string
  parentId?: string | null
  isActive: boolean
}

export type RequestDto = {
  id: string
  customerId: string
  categoryId?: string | null
  categoryName?: string | null
  description: string
  preferredStart?: string | null
  preferredEnd?: string | null
  budgetAmount?: number | null
  serviceArea?: string | null
  address?: string | null
  latitude?: number | null
  longitude?: number | null
  status: string
  version: number
  createdAt: string
  updatedAt: string
}

export type RequestWriteRequest = {
  categoryId?: string
  description: string
  preferredStart?: string
  preferredEnd?: string
  budgetAmount?: number
  serviceArea?: string
  address?: string
  latitude?: number
  longitude?: number
}

export type RequestHistoryDto = {
  id: string
  fromStatus: string
  toStatus: string
  timestamp: string
  note?: string | null
}

export type MediaDto = {
  id: string
  storageKey: string
  mimeType: string
  uploadedAt: string
}

export type TechnicianProfileDto = {
  id: string
  userId: string
  displayName: string
  bio?: string | null
  serviceArea?: string | null
  latitudeApprox?: number | null
  longitudeApprox?: number | null
  experienceSummary?: string | null
  isSuspended: boolean
  averageRating: number
  reviewCount: number
  approvedCategories: string[]
  completedJobs?: number
  profilePhotoUrl?: string | null
}

export type TechnicianProfileRequest = {
  bio?: string
  serviceArea?: string
  latitudeApprox?: number
  longitudeApprox?: number
  experienceSummary?: string
}

export type TechnicianApplicationDto = {
  id: string
  technicianId: string
  categoryId: string
  categoryName?: string | null
  status: string
  submittedAt: string
  decidedAt?: string | null
  decisionNotes?: string | null
  version: number
}

export type DocumentDto = {
  id: string
  evidenceType: string
  storageKey: string
  mimeType: string
  reviewStatus: string
  uploadedAt: string
}

export type InvitationDto = {
  id: string
  requestId: string
  technicianId: string
  status: string
  sentAt: string
  serviceArea?: string | null
  categoryName?: string | null
  description: string
}

export type QuoteDto = {
  id: string
  requestId: string
  technicianId: string
  quoteGroupId: string
  labourAmount: number
  materialsAmount: number
  travelAmount: number
  totalAmount: number
  currency: string
  durationMinutes: number
  arrivalStart?: string | null
  expiresAt: string
  assumptions?: string | null
  includedMaterials?: string | null
  excludedMaterials?: string | null
  status: string
  version: number
  approximateDistanceKm?: number | null
  distanceUnavailable?: boolean
  distanceBand?: string | null
  technicianDisplayName?: string | null
  averageRating?: number | null
  reviewCount?: number | null
  completedJobs?: number | null
  recommendationSummary?: string | null
  strengths?: string[]
  tradeoffs?: string[]
  profilePhotoUrl?: string | null
}

export type QuoteWriteRequest = {
  labourAmount: number
  materialsAmount: number
  travelAmount: number
  totalAmount: number
  currency: string
  durationMinutes?: number
  arrivalStart?: string
  expiresAt?: string
  assumptions?: string
  includedMaterials?: string
  excludedMaterials?: string
}

export type BookingDto = {
  id: string
  requestId: string
  quotationId: string
  customerId: string
  technicianId: string
  status: string
  approvedAt?: string | null
  confirmedAt?: string | null
  addressReleaseAt?: string | null
  address?: string | null
  latitude?: number | null
  longitude?: number | null
  technicianDisplayName?: string | null
  customerDisplayName?: string | null
  customerPhone?: string | null
  categoryName?: string | null
  requestDescription?: string | null
  serviceArea?: string | null
  preferredStart?: string | null
  quoteTotalAmount?: number | null
  currency?: string | null
  profilePhotoUrl?: string | null
  version: number
}

export type BookingHistoryDto = {
  id: string
  fromStatus: string
  toStatus: string
  note?: string | null
  timestamp: string
}

export type ScopeChangeDto = {
  id: string
  bookingId: string
  proposedCost: number
  description: string
  customerDecision: string
  decisionAt?: string | null
}

export type ReviewDto = {
  id: string
  bookingId: string
  customerId: string
  technicianId: string
  customerDisplayName?: string | null
  technicianDisplayName?: string | null
  rating: number
  body?: string | null
  status: string
  createdAt: string
  moderationReason?: string | null
  adminReply?: string | null
  adminRepliedAt?: string | null
}

export type ComplaintDto = {
  id: string
  bookingId?: string | null
  reportedById: string
  technicianDisplayName?: string | null
  customerDisplayName?: string | null
  subject: string
  description: string
  resolution?: string | null
  adminReply?: string | null
  adminRepliedAt?: string | null
  status: string
  createdAt: string
  resolvedAt?: string | null
}

export type AdminUserDto = {
  id: string
  email: string
  displayName: string
  phone?: string | null
  role: string
  isActive: boolean
  createdAt: string
  technicianProfileId?: string | null
  address?: string | null
  serviceArea?: string | null
  requestedCategory?: string | null
  loginStatus: string
}

export type AdminCreateUserRequest = {
  email: string
  password: string
  displayName: string
  phone?: string
  role: 'CUSTOMER' | 'TECHNICIAN'
  address?: string
  serviceArea?: string
  categoryId?: string
}

export type NotificationDto = {
  id: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
}

export type PublicTechnicianDto = {
  id: string
  displayName: string
  approvedCategories: string[]
  categoryVerified: boolean
  averageRating: number
  reviewCount: number
  completedJobs: number
  serviceSummary?: string | null
  profilePhotoUrl?: string | null
}

export type DashboardDto = {
  totalCustomers: number
  totalTechnicians: number
  pendingVerification: number
  approvedTechnicians: number
  rejectedApplications: number
  totalRequests: number
  openRequests: number
  totalBookings: number
  activeBookings: number
  completedJobs: number
  cancelledJobs: number
  technicians: number
  pendingApplications: number
  openComplaints: number
  aiWorkflows: number
  averageRating: number
  aiWorkflowSuccessRate: number
}

export type RequestReportDto = {
  id: string
  status: string
  categoryName?: string | null
  createdAt: string
}

export type BookingReportDto = {
  id: string
  status: string
  technicianId: string
  confirmedAt?: string | null
}

export type TechnicianReportDto = {
  id: string
  displayName: string
  averageRating: number
  reviewCount: number
  isSuspended: boolean
}

export type AgentReportDto = {
  id: string
  requestId: string
  status: string
  approvalStatus: string
  startedAt: string
}

export type WorkflowDto = {
  id: string
  requestId: string
  objective: string
  planJson: string
  currentStep: string
  status: string
  approvalStatus: string
  startedAt: string
  finishedAt?: string | null
}

export type WorkflowStepDto = {
  id: string
  agentRole: string
  inputSummary: string
  outputJson: string
  toolName?: string | null
  toolResultSummary?: string | null
  durationMs: number
  timestamp: string
}

export type AuditLogDto = {
  id: string
  actorId?: string | null
  actor?: string | null
  action: string
  entity: string
  entityId?: string | null
  outcome: string
  timestamp: string
}

export type CategoryRequirementDraft = {
  id: string
  categoryId: string
  evidenceType: string
  isRequired: boolean
  validationMethod: string
  ruleVersion: string
}
