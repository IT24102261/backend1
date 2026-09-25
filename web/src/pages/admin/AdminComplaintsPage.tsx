import { useEffect, useMemo, useState } from 'react'
import { complaintsApi } from '../../api/complaints'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { FormField, SelectInput, TextArea } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import { useToastStore } from '../../store/toastStore'
import type { ComplaintDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminComplaintsPage() {
  const query = usePagedQuery()
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<ComplaintDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<ComplaintDto | null>(null)
  const [status, setStatus] = useState('IN_REVIEW')
  const [resolution, setResolution] = useState('')
  const [reply, setReply] = useState('')

  function load() {
    setLoading(true)
    complaintsApi
      .adminList(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(load, [query.page, query.search, query.status])

  function open(row: ComplaintDto) {
    setSelected(row)
    setStatus(row.status)
    setResolution(row.resolution ?? '')
    setReply(row.adminReply ?? '')
  }

  const columns = useMemo<Column<ComplaintDto>[]>(
    () => [
      { key: 'subject', header: 'Subject', render: (row) => row.subject },
      { key: 'technicianDisplayName', header: 'Technician', render: (row) => row.technicianDisplayName || '—' },
      { key: 'bookingId', header: 'Related booking', render: (row) => shortId(row.bookingId) },
      { key: 'adminReply', header: 'Admin reply', render: (row) => row.adminReply || '—' },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
      {
        key: 'action',
        header: 'Action',
        render: (row) => (
          <button type="button" className="font-medium text-[#c4a574] hover:text-[#171717]" onClick={() => open(row)}>
            Reply
          </button>
        ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Complaints" description="Reply to customer complaints and record a resolution status." />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search subject" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'OPEN', label: 'Open' },
            { value: 'IN_REVIEW', label: 'In review' },
            { value: 'RESOLVED', label: 'Resolved' },
            { value: 'DISMISSED', label: 'Dismissed' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No complaints"
        emptyDescription="No complaints match these filters."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        onPageChange={query.setPage}
      />
      {selected ? (
        <div className="space-y-4 rounded-2xl border border-black/8 bg-white p-5">
          <div>
            <h2 className="font-semibold text-[#171717]">{selected.subject}</h2>
            <p className="mt-2 text-sm text-[#6d6a64]">{selected.description}</p>
            <p className="mt-2 text-xs text-[#9a968e]">
              Booking {shortId(selected.bookingId)}
              {selected.technicianDisplayName ? ` · ${selected.technicianDisplayName}` : ''}
              {selected.customerDisplayName ? ` · ${selected.customerDisplayName}` : ''}
            </p>
          </div>
          <FormField label="Reply to customer">
            <TextArea value={reply} onChange={(event) => setReply(event.target.value)} />
          </FormField>
          <div className="grid gap-4 md:grid-cols-2">
            <FormField label="Status">
              <SelectInput value={status} onChange={(event) => setStatus(event.target.value)}>
                <option value="OPEN">Open</option>
                <option value="IN_REVIEW">In review</option>
                <option value="RESOLVED">Resolved</option>
                <option value="DISMISSED">Dismissed</option>
              </SelectInput>
            </FormField>
            <FormField label="Resolution note">
              <TextArea value={resolution} onChange={(event) => setResolution(event.target.value)} />
            </FormField>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              onClick={async () => {
                try {
                  const updated = await complaintsApi.reply(selected.id, reply)
                  setSelected(updated)
                  push('success', 'Reply sent to the customer.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Send reply
            </Button>
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  await complaintsApi.updateStatus(selected.id, status, resolution)
                  push('success', 'Complaint status updated.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Save status
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  )
}
