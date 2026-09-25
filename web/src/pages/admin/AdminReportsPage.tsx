import { useEffect, useState } from 'react'
import { Download } from 'lucide-react'
import { adminApi } from '../../api/admin'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { AgentReportDto, BookingReportDto, DashboardDto, RequestReportDto, TechnicianReportDto } from '../../types/api'
import { exportAdminReportsPdf } from '../../utils/exportReportsPdf'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminReportsPage() {
  const push = useToastStore((state) => state.push)
  const [error, setError] = useState('')
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null)
  const [requests, setRequests] = useState<RequestReportDto[]>([])
  const [bookings, setBookings] = useState<BookingReportDto[]>([])
  const [technicians, setTechnicians] = useState<TechnicianReportDto[]>([])
  const [agents, setAgents] = useState<AgentReportDto[]>([])
  const [exporting, setExporting] = useState(false)

  useEffect(() => {
    Promise.all([
      adminApi.dashboard(),
      adminApi.requestReports({ page: 1, pageSize: 100 }),
      adminApi.bookingReports({ page: 1, pageSize: 100 }),
      adminApi.technicianReports({ page: 1, pageSize: 100 }),
      adminApi.agentReports({ page: 1, pageSize: 100 }),
    ])
      .then(([dash, req, book, tech, agent]) => {
        setDashboard(dash)
        setRequests(req.items)
        setBookings(book.items)
        setTechnicians(tech.items)
        setAgents(agent.items)
      })
      .catch((err) => setError(getApiError(err).error))
  }, [])

  function exportPdf() {
    if (!dashboard) {
      push('error', 'Reports are still loading.')
      return
    }
    setExporting(true)
    try {
      exportAdminReportsPdf({ dashboard, requests, bookings, technicians, agents })
      push('success', 'Report PDF downloaded.')
    } catch (err) {
      push('error', err instanceof Error ? err.message : 'Could not export the PDF.')
    } finally {
      setExporting(false)
    }
  }

  const requestColumns: Column<RequestReportDto>[] = [
    { key: 'id', header: 'Request', render: (row) => shortId(row.id) },
    { key: 'categoryName', header: 'Category', render: (row) => row.categoryName || '—' },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
  ]

  return (
    <div className="space-y-5">
      <PageHeader
        title="Reports"
        description="All figures are read from PostgreSQL through the admin report APIs."
        actions={
          <Button size="sm" onClick={exportPdf} disabled={!dashboard || exporting}>
            <Download size={14} />
            {exporting ? 'Preparing PDF…' : 'Export PDF'}
          </Button>
        }
      />
      {error ? <ErrorState message={error} /> : null}
      {dashboard ? (
        <dl className="grid gap-3 rounded-2xl border border-black/8 bg-white p-5 text-sm md:grid-cols-4">
          <div>
            <dt className="text-slate-400">Customers</dt>
            <dd className="font-semibold">{dashboard.totalCustomers}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Completed jobs</dt>
            <dd className="font-semibold">{dashboard.completedJobs}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Cancelled jobs</dt>
            <dd className="font-semibold">{dashboard.cancelledJobs}</dd>
          </div>
          <div>
            <dt className="text-slate-400">AI success</dt>
            <dd className="font-semibold">{dashboard.aiWorkflowSuccessRate}%</dd>
          </div>
        </dl>
      ) : null}
      <DataTable columns={requestColumns} rows={requests} rowKey={(row) => row.id} emptyTitle="No request rows" page={1} pageSize={50} totalCount={requests.length} onPageChange={() => undefined} />
      <DataTable
        columns={[
          { key: 'id', header: 'Booking', render: (row) => shortId(row.id) },
          { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
        ]}
        rows={bookings}
        rowKey={(row) => row.id}
        emptyTitle="No booking rows"
        page={1}
        pageSize={50}
        totalCount={bookings.length}
        onPageChange={() => undefined}
      />
      <DataTable
        columns={[
          { key: 'displayName', header: 'Technician', render: (row) => row.displayName },
          { key: 'averageRating', header: 'Rating', render: (row) => Number(row.averageRating).toFixed(1) },
        ]}
        rows={technicians}
        rowKey={(row) => row.id}
        emptyTitle="No technician rows"
        page={1}
        pageSize={50}
        totalCount={technicians.length}
        onPageChange={() => undefined}
      />
      <DataTable
        columns={[
          { key: 'id', header: 'Workflow', render: (row) => shortId(row.id) },
          { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
        ]}
        rows={agents}
        rowKey={(row) => row.id}
        emptyTitle="No workflow rows"
        page={1}
        pageSize={50}
        totalCount={agents.length}
        onPageChange={() => undefined}
      />
    </div>
  )
}
