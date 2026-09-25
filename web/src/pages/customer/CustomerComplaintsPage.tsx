import { useEffect, useState, type FormEvent } from 'react'
import { complaintsApi } from '../../api/complaints'
import { marketplaceApi } from '../../api/marketplace'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { BookingDto, ComplaintDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function CustomerComplaintsPage() {
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<ComplaintDto[]>([])
  const [bookings, setBookings] = useState<BookingDto[]>([])
  const [bookingId, setBookingId] = useState('')
  const [subject, setSubject] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')

  function load() {
    Promise.all([complaintsApi.mine({ page: 1, pageSize: 20 }), marketplaceApi.bookings({ page: 1, pageSize: 50 })])
      .then(([complaints, result]) => {
        setRows(complaints.items)
        setBookings(result.items)
      })
      .catch((err) => setError(getApiError(err).error))
  }

  useEffect(load, [])

  async function create(event: FormEvent) {
    event.preventDefault()
    try {
      await complaintsApi.create({ bookingId, subject, description })
      push('success', 'Complaint submitted.')
      setSubject('')
      setDescription('')
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  const columns: Column<ComplaintDto>[] = [
    { key: 'subject', header: 'Subject', render: (row) => row.subject },
    { key: 'bookingId', header: 'Booking', render: (row) => shortId(row.bookingId) },
    { key: 'technicianDisplayName', header: 'Technician', render: (row) => row.technicianDisplayName || '—' },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'adminReply', header: 'Admin reply', render: (row) => row.adminReply || '—' },
    { key: 'resolution', header: 'Resolution', render: (row) => row.resolution || '—' },
    { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Complaints" description="Choose the booking and the technician who did the work. Only an admin can reply." />
      {error ? <ErrorState message={error} /> : null}
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={create}>
        <FormField label="Booking">
          <SelectInput value={bookingId} onChange={(event) => setBookingId(event.target.value)}>
            <option value="">Select booking and technician</option>
            {bookings.map((item) => (
              <option key={item.id} value={item.id}>
                {shortId(item.id)} · {item.technicianDisplayName || 'Technician'}
              </option>
            ))}
          </SelectInput>
        </FormField>
        <FormField label="Subject">
          <TextInput value={subject} onChange={(event) => setSubject(event.target.value)} />
        </FormField>
        <FormField label="Description">
          <TextArea value={description} onChange={(event) => setDescription(event.target.value)} />
        </FormField>
        <div className="flex items-end">
          <Button type="submit" size="sm">
            Create complaint
          </Button>
        </div>
      </form>
      <DataTable columns={columns} rows={rows} rowKey={(row) => row.id} emptyTitle="No complaints" emptyDescription="Open a complaint from a booking if something went wrong." page={1} pageSize={20} totalCount={rows.length} onPageChange={() => undefined} />
    </div>
  )
}
