import { jsPDF } from 'jspdf'
import autoTable, { type UserOptions } from 'jspdf-autotable'
import type { AgentReportDto, BookingReportDto, DashboardDto, RequestReportDto, TechnicianReportDto } from '../types/api'
import { formatDate, shortId } from './format'

export function exportAdminReportsPdf(data: {
  dashboard: DashboardDto
  requests: RequestReportDto[]
  bookings: BookingReportDto[]
  technicians: TechnicianReportDto[]
  agents: AgentReportDto[]
}) {
  const doc = new jsPDF({ orientation: 'portrait', unit: 'pt', format: 'a4' })
  const generated = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date())
  let cursorY = 96

  doc.setFillColor(17, 19, 24)
  doc.rect(0, 0, doc.internal.pageSize.getWidth(), 72, 'F')
  doc.setFillColor(196, 165, 116)
  doc.rect(0, 72, doc.internal.pageSize.getWidth(), 4, 'F')
  doc.setTextColor(255, 255, 255)
  doc.setFontSize(18)
  doc.text('FixFlow AI', 40, 34)
  doc.setFontSize(11)
  doc.text('Admin operations report', 40, 52)
  doc.setFontSize(9)
  doc.text(`Generated ${generated}`, doc.internal.pageSize.getWidth() - 40, 52, { align: 'right' })

  function addTable(title: string, options: UserOptions) {
    doc.setTextColor(23, 23, 23)
    doc.setFontSize(12)
    doc.text(title, 40, cursorY)
    cursorY += 12
    autoTable(doc, {
      ...options,
      startY: cursorY,
      theme: 'striped',
      headStyles: { fillColor: [17, 19, 24], textColor: 255, fontStyle: 'bold' },
      alternateRowStyles: { fillColor: [250, 247, 241] },
      styles: { fontSize: 8, cellPadding: 5, textColor: [23, 23, 23] },
      margin: { left: 40, right: 40 },
      didDrawPage(hook) {
        cursorY = (hook.cursor?.y ?? cursorY) + 28
      },
    })
  }

  addTable('Summary', {
    head: [['Metric', 'Value']],
    body: [
      ['Customers', String(data.dashboard.totalCustomers)],
      ['Technicians', String(data.dashboard.totalTechnicians || data.dashboard.technicians)],
      ['Pending verification', String(data.dashboard.pendingVerification || data.dashboard.pendingApplications)],
      ['Approved technicians', String(data.dashboard.approvedTechnicians)],
      ['Total requests', String(data.dashboard.totalRequests)],
      ['Open requests', String(data.dashboard.openRequests)],
      ['Active bookings', String(data.dashboard.activeBookings)],
      ['Completed jobs', String(data.dashboard.completedJobs)],
      ['Cancelled jobs', String(data.dashboard.cancelledJobs)],
      ['Open complaints', String(data.dashboard.openComplaints)],
      ['Average rating', String(data.dashboard.averageRating)],
      ['AI workflow success', `${data.dashboard.aiWorkflowSuccessRate}%`],
    ],
  })

  addTable('Requests', {
    head: [['Request', 'Category', 'Status', 'Created']],
    body: data.requests.map((row) => [shortId(row.id), row.categoryName || '—', row.status.replaceAll('_', ' '), formatDate(row.createdAt)]),
  })

  addTable('Bookings', {
    head: [['Booking', 'Status', 'Confirmed']],
    body: data.bookings.map((row) => [shortId(row.id), row.status.replaceAll('_', ' '), formatDate(row.confirmedAt)]),
  })

  addTable('Technicians', {
    head: [['Technician', 'Rating', 'Reviews', 'Status']],
    body: data.technicians.map((row) => [
      row.displayName,
      Number(row.averageRating).toFixed(1),
      String(row.reviewCount),
      row.isSuspended ? 'Suspended' : 'Active',
    ]),
  })

  addTable('AI workflows', {
    head: [['Workflow', 'Request', 'Status', 'Approval', 'Started']],
    body: data.agents.map((row) => [
      shortId(row.id),
      shortId(row.requestId),
      row.status.replaceAll('_', ' '),
      row.approvalStatus.replaceAll('_', ' '),
      formatDate(row.startedAt),
    ]),
  })

  const stamp = new Date().toISOString().slice(0, 10)
  doc.save(`fixflow-admin-report-${stamp}.pdf`)
}
