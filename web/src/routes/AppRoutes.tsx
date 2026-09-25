import { Route, Routes } from 'react-router-dom'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { PublicLayout } from '../layouts/PublicLayout'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { AdminAuditPage } from '../pages/admin/AdminAuditPage'
import { AdminBookingsPage } from '../pages/admin/AdminBookingsPage'
import { AdminCategoriesPage } from '../pages/admin/AdminCategoriesPage'
import { AdminComplaintsPage } from '../pages/admin/AdminComplaintsPage'
import { AdminDashboardPage } from '../pages/admin/AdminDashboardPage'
import { AdminRequestsPage } from '../pages/admin/AdminRequestsPage'
import { AdminReviewsPage } from '../pages/admin/AdminReviewsPage'
import { AdminTechniciansPage } from '../pages/admin/AdminTechniciansPage'
import { AdminUsersPage } from '../pages/admin/AdminUsersPage'
import { AdminVerificationDetailPage } from '../pages/admin/AdminVerificationDetailPage'
import { AdminVerificationsPage } from '../pages/admin/AdminVerificationsPage'
import { AdminWorkflowDetailPage } from '../pages/admin/AdminWorkflowDetailPage'
import { AdminWorkflowsPage } from '../pages/admin/AdminWorkflowsPage'
import { AdminReportsPage } from '../pages/admin/AdminReportsPage'
import { CustomerBookingsPage } from '../pages/customer/CustomerBookingsPage'
import { CustomerComplaintsPage } from '../pages/customer/CustomerComplaintsPage'
import { CustomerDashboardPage } from '../pages/customer/CustomerDashboardPage'
import { CustomerNotificationsPage } from '../pages/customer/CustomerNotificationsPage'
import { CustomerProfilePage } from '../pages/customer/CustomerProfilePage'
import { CustomerRequestsPage } from '../pages/customer/CustomerRequestsPage'
import { CustomerReviewsPage } from '../pages/customer/CustomerReviewsPage'
import { NotFoundPage } from '../pages/public/NotFoundPage'
import { TechnicianPublicPage } from '../pages/public/TechnicianPublicPage'
import { UnauthorizedPage } from '../pages/public/UnauthorizedPage'
import { TechnicianDashboardPage } from '../pages/technician/TechnicianDashboardPage'
import { TechnicianInvitationsPage } from '../pages/technician/TechnicianInvitationsPage'
import { TechnicianJobsPage } from '../pages/technician/TechnicianJobsPage'
import { TechnicianProfilePage } from '../pages/technician/TechnicianProfilePage'
import { TechnicianQuotationsPage } from '../pages/technician/TechnicianQuotationsPage'
import { TechnicianReviewsPage } from '../pages/technician/TechnicianReviewsPage'
import { TechnicianComplaintsPage } from '../pages/technician/TechnicianComplaintsPage'
import { TechnicianVerificationPage } from '../pages/technician/TechnicianVerificationPage'
import { ProtectedRoute } from './ProtectedRoute'
import { RoleRoute } from './RoleRoute'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/technicians/:id" element={<TechnicianPublicPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route path="admin" element={<RoleRoute roles={['ADMIN']} />}>
          <Route element={<DashboardLayout variant="admin" />}>
            <Route path="dashboard" element={<AdminDashboardPage />} />
            <Route path="users" element={<AdminUsersPage />} />
            <Route path="verifications" element={<AdminVerificationsPage />} />
            <Route path="verifications/:id" element={<AdminVerificationDetailPage />} />
            <Route path="categories" element={<AdminCategoriesPage />} />
            <Route path="technicians" element={<AdminTechniciansPage />} />
            <Route path="requests" element={<AdminRequestsPage />} />
            <Route path="bookings" element={<AdminBookingsPage />} />
            <Route path="reviews" element={<AdminReviewsPage />} />
            <Route path="complaints" element={<AdminComplaintsPage />} />
            <Route path="ai-workflows" element={<AdminWorkflowsPage />} />
            <Route path="ai-workflows/:id" element={<AdminWorkflowDetailPage />} />
            <Route path="audit" element={<AdminAuditPage />} />
            <Route path="reports" element={<AdminReportsPage />} />
          </Route>
        </Route>

        <Route path="technician" element={<RoleRoute roles={['TECHNICIAN']} />}>
          <Route element={<DashboardLayout variant="technician" />}>
            <Route path="dashboard" element={<TechnicianDashboardPage />} />
            <Route path="verification" element={<TechnicianVerificationPage />} />
            <Route path="invitations" element={<TechnicianInvitationsPage />} />
            <Route path="quotations" element={<TechnicianQuotationsPage />} />
            <Route path="jobs" element={<TechnicianJobsPage />} />
            <Route path="profile" element={<TechnicianProfilePage />} />
            <Route path="reviews" element={<TechnicianReviewsPage />} />
            <Route path="complaints" element={<TechnicianComplaintsPage />} />
            <Route path="notifications" element={<CustomerNotificationsPage />} />
          </Route>
        </Route>

        <Route path="customer" element={<RoleRoute roles={['CUSTOMER']} />}>
          <Route element={<DashboardLayout variant="customer" />}>
            <Route path="dashboard" element={<CustomerDashboardPage />} />
            <Route path="requests" element={<CustomerRequestsPage />} />
            <Route path="bookings" element={<CustomerBookingsPage />} />
            <Route path="reviews" element={<CustomerReviewsPage />} />
            <Route path="complaints" element={<CustomerComplaintsPage />} />
            <Route path="profile" element={<CustomerProfilePage />} />
            <Route path="notifications" element={<CustomerNotificationsPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
