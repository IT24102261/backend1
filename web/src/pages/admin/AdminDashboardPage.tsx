import { useEffect, useState } from 'react'
import { Activity, Briefcase, CheckCircle2, ClipboardList, MessageSquareWarning, ShieldCheck, Star, Users, Workflow, Wrench } from 'lucide-react'
import { adminApi } from '../../api/admin'
import { requestsApi } from '../../api/requests'
import { BarChart, DonutChart } from '../../components/ui/Charts'
import { DashboardSection, QuickAction } from '../../components/ui/DashboardPanel'
import { CardSkeleton } from '../../components/ui/Skeleton'
import { ErrorState } from '../../components/ui/ErrorState'
import { WorkspaceBanner } from '../../components/ui/WorkspaceBanner'
import { StatCard } from '../../components/ui/StatCard'
import type { AgentReportDto, BookingReportDto, DashboardDto, RequestDto, RequestReportDto, TechnicianReportDto } from '../../types/api'
import { getApiError } from '../../utils/errors'

export function AdminDashboardPage() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null)
  const [requests, setRequests] = useState<RequestReportDto[]>([])
  const [bookings, setBookings] = useState<BookingReportDto[]>([])
  const [technicians, setTechnicians] = useState<TechnicianReportDto[]>([])
  const [agents, setAgents] = useState<AgentReportDto[]>([])
  const [customers, setCustomers] = useState(0)

  useEffect(() => {
    Promise.all([
      adminApi.dashboard(),
      adminApi.requestReports({ page: 1, pageSize: 100 }),
      adminApi.bookingReports({ page: 1, pageSize: 100 }),
      adminApi.technicianReports({ page: 1, pageSize: 100 }),
      adminApi.agentReports({ page: 1, pageSize: 100 }),
      requestsApi.list({ page: 1, pageSize: 100 }),
    ])
      .then(([dash, req, book, tech, agent, liveRequests]) => {
        setDashboard(dash)
        setRequests(req.items)
        setBookings(book.items)
        setTechnicians(tech.items)
        setAgents(agent.items)
        setCustomers(new Set(liveRequests.items.map((item: RequestDto) => item.customerId)).size)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [])

  const completedJobs = bookings.filter((item) => item.status === 'CLOSED' || item.status === 'CUSTOMER_CONFIRMED').length
  const averageRating = technicians.length
    ? (technicians.reduce((sum, item) => sum + Number(item.averageRating), 0) / technicians.length).toFixed(1)
    : '—'
  const byCategory = Object.entries(
    requests.reduce<Record<string, number>>((acc, item) => {
      const key = item.categoryName || 'Uncategorised'
      acc[key] = (acc[key] ?? 0) + 1
      return acc
    }, {}),
  ).map(([label, value]) => ({ label, value }))

  const bookingsByMonth = Object.entries(
    bookings.reduce<Record<string, number>>((acc, item) => {
      if (!item.confirmedAt) return acc
      const key = new Date(item.confirmedAt).toLocaleString('en-GB', { month: 'short', year: 'numeric' })
      acc[key] = (acc[key] ?? 0) + 1
      return acc
    }, {}),
  ).map(([label, value]) => ({ label, value }))

  const verification = [
    { label: 'Active', value: technicians.filter((item) => !item.isSuspended).length, color: '#111318' },
    { label: 'Suspended', value: technicians.filter((item) => item.isSuspended).length, color: '#e11d48' },
    { label: 'Pending applications', value: dashboard?.pendingApplications ?? 0, color: '#c4a574' },
  ]

  const workflowStatus = Object.entries(
    agents.reduce<Record<string, number>>((acc, item) => {
      acc[item.status] = (acc[item.status] ?? 0) + 1
      return acc
    }, {}),
  ).map(([label, value], index) => ({
    label,
    value,
    color: ['#111318', '#c4a574', '#9b7d4e', '#e11d48', '#6d6a64'][index % 5],
  }))

  return (
    <div className="space-y-8">
      <WorkspaceBanner
        kicker="Admin workspace"
        title="Operations overview"
        description="Watch users, verifications, bookings, and AI workflow health from one place."
      />

      {loading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, index) => (
            <CardSkeleton key={index} />
          ))}
        </div>
      ) : null}
      {error ? <ErrorState message={error} /> : null}

      {dashboard ? (
        <>
          <div className="grid gap-4 lg:grid-cols-3">
            <QuickAction to="/admin/users" icon={Users} title="Manage users" description="Add customers or technicians and approve login." />
            <QuickAction to="/admin/verifications" icon={ShieldCheck} title="Verifications" description="Review category applications and documents." />
            <QuickAction to="/admin/complaints" icon={MessageSquareWarning} title="Complaints" description="Reply to customer issues. Technicians cannot reply." />
          </div>

          <div>
            <p className="mb-3 text-xs font-medium uppercase tracking-[0.18em] text-[#9a968e]">People</p>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <StatCard label="Customers" value={dashboard.totalCustomers || customers} icon={Users} />
              <StatCard label="Technicians" value={dashboard.totalTechnicians || dashboard.technicians} icon={Wrench} accent="navy" />
              <StatCard label="Pending verification" value={dashboard.pendingVerification || dashboard.pendingApplications} hint="Category applications waiting" icon={ShieldCheck} accent="cream" />
              <StatCard label="Approved technicians" value={dashboard.approvedTechnicians} icon={CheckCircle2} />
            </div>
          </div>

          <div>
            <p className="mb-3 text-xs font-medium uppercase tracking-[0.18em] text-[#9a968e]">Work</p>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <StatCard label="Requests" value={dashboard.totalRequests} hint={`${dashboard.openRequests} still open`} icon={ClipboardList} />
              <StatCard label="Active bookings" value={dashboard.activeBookings} icon={Briefcase} accent="navy" />
              <StatCard label="Completed jobs" value={dashboard.completedJobs || completedJobs} icon={CheckCircle2} />
              <StatCard label="Cancelled jobs" value={dashboard.cancelledJobs} icon={Briefcase} accent="cream" />
            </div>
          </div>

          <div>
            <p className="mb-3 text-xs font-medium uppercase tracking-[0.18em] text-[#9a968e]">Quality</p>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <StatCard label="Average rating" value={dashboard.averageRating || averageRating} icon={Star} />
              <StatCard
                label="AI workflow success"
                value={`${dashboard.aiWorkflowSuccessRate}%`}
                hint={`${dashboard.aiWorkflows} workflows`}
                icon={Workflow}
                accent="navy"
              />
              <StatCard label="Open complaints" value={dashboard.openComplaints} hint="Admin reply only" icon={MessageSquareWarning} accent="cream" />
            </div>
          </div>

          <div className="grid gap-5 lg:grid-cols-2">
            <DashboardSection title="Requests by category" description="Where customers are asking for help.">
              <BarChart data={byCategory} />
            </DashboardSection>
            <DashboardSection title="Bookings per month" description="Confirmed jobs over time.">
              <BarChart data={bookingsByMonth} />
            </DashboardSection>
            <DashboardSection title="Technician verification" description="Active, suspended, and pending applications.">
              <DonutChart data={verification} />
            </DashboardSection>
            <DashboardSection
              title="AI workflow status"
              description="Planning, matching, recommendation, and validation."
              action={<Activity size={18} className="text-[#c4a574]" />}
            >
              <DonutChart data={workflowStatus} />
            </DashboardSection>
          </div>
        </>
      ) : null}
    </div>
  )
}
