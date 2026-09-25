import { useEffect, useState } from 'react'
import { complaintsApi } from '../../api/complaints'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import type { ComplaintDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function TechnicianComplaintsPage() {
  const [rows, setRows] = useState<ComplaintDto[]>([])
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<ComplaintDto | null>(null)

  useEffect(() => {
    complaintsApi
      .mine({ page: 1, pageSize: 50 })
      .then((result) => setRows(result.items))
      .catch((err) => setError(getApiError(err).error))
  }, [])

  const columns: Column<ComplaintDto>[] = [
    { key: 'subject', header: 'Subject', render: (row) => row.subject },
    { key: 'bookingId', header: 'Booking', render: (row) => shortId(row.bookingId) },
    { key: 'customerDisplayName', header: 'Customer', render: (row) => row.customerDisplayName || '—' },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'adminReply', header: 'Admin reply', render: (row) => row.adminReply || '—' },
    { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
    {
      key: 'action',
      header: '',
      render: (row) => (
        <button type="button" className="font-medium text-[#c4a574] hover:text-[#171717]" onClick={() => setSelected(row)}>
          View
        </button>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <PageHeader
        title="Complaints"
        description="Customer complaints about your jobs appear here and in Notifications. Only an admin can reply."
      />
      {error ? <ErrorState message={error} /> : null}
      {rows.length === 0 && !error ? (
        <EmptyState title="No complaints" description="If a customer files a complaint on one of your jobs, it will appear here." />
      ) : (
        <DataTable
          columns={columns}
          rows={rows}
          rowKey={(row) => row.id}
          emptyTitle="No complaints"
          emptyDescription="No complaints on your jobs yet."
          page={1}
          pageSize={50}
          totalCount={rows.length}
          onPageChange={() => undefined}
        />
      )}
      {selected ? (
        <section className="space-y-3 rounded-2xl border border-black/8 bg-white p-5">
          <h2 className="font-semibold text-[#171717]">{selected.subject}</h2>
          <p className="text-sm text-[#6d6a64]">
            Booking {shortId(selected.bookingId)}
            {selected.customerDisplayName ? ` · ${selected.customerDisplayName}` : ''}
          </p>
          <p className="text-sm leading-6 text-[#171717]">{selected.description}</p>
          {selected.adminReply ? (
            <p className="rounded-xl bg-[#f4efe6] px-3 py-2 text-sm text-[#171717]">
              <span className="font-medium">Admin reply:</span> {selected.adminReply}
            </p>
          ) : (
            <p className="text-xs text-[#9a968e]">Technicians cannot reply. An admin will respond if needed.</p>
          )}
        </section>
      ) : null}
    </div>
  )
}
