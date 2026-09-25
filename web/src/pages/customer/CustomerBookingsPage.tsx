import { useEffect, useState } from 'react'
import { complaintsApi } from '../../api/complaints'
import { marketplaceApi } from '../../api/marketplace'
import { Button } from '../../components/ui/Button'
import { BookingTracker } from '../../components/ui/BookingTracker'
import { LocationMap } from '../../components/ui/LocationMap'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
import { useToastStore } from '../../store/toastStore'
import type { BookingDto, BookingHistoryDto, ScopeChangeDto } from '../../types/api'
import { formatBookingStatus } from '../../utils/bookingStatus'
import { formatDate, formatMoney, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function CustomerBookingsPage() {
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<BookingDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [history, setHistory] = useState<BookingHistoryDto[]>([])
  const [scope, setScope] = useState<ScopeChangeDto[]>([])
  const [selected, setSelected] = useState<BookingDto | null>(null)

  function load() {
    setLoading(true)
    marketplaceApi
      .bookings({ page, pageSize: 10 })
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
        setSelected((current) => result.items.find((item) => item.id === current?.id) ?? current)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(load, [page])

  const columns: Column<BookingDto>[] = [
    { key: 'id', header: 'Booking', render: (row) => shortId(row.id) },
    { key: 'technicianId', header: 'Technician', render: (row) => (
        <span className="inline-flex items-center gap-2">
          <TechnicianAvatar name={row.technicianDisplayName} photoUrl={row.profilePhotoUrl} size={36} />
          {row.technicianDisplayName || shortId(row.technicianId)}
        </span>
      ) },
    {
      key: 'quoteTotalAmount',
      header: 'Quotation',
      render: (row) =>
        row.quoteTotalAmount != null ? formatMoney(row.quoteTotalAmount, row.currency || 'LKR') : '—',
    },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} label={formatBookingStatus(row.status)} /> },
    { key: 'confirmedAt', header: 'Confirmed', render: (row) => formatDate(row.confirmedAt) },
    {
      key: 'action',
      header: '',
      render: (row) => (
        <div className="flex gap-2">
          {row.status === 'PENDING_VALIDATION' ? (
            <Button
              onClick={async () => {
                try {
                  await marketplaceApi.confirmBooking(row.id)
                  push('success', 'Booking confirmed. Address released to the technician.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                  load()
                }
              }}
            >
              Confirm booking
            </Button>
          ) : null}
          <Button
            variant="ghost"
            onClick={async () => {
              try {
                setSelected(row)
                setHistory(await marketplaceApi.bookingHistory(row.id))
                setScope(await marketplaceApi.scopeChanges(row.id))
              } catch (err) {
                push('error', getApiError(err).error)
              }
            }}
          >
            Track job
          </Button>
          {['CONFIRMED', 'ACCEPTED', 'EN_ROUTE', 'IN_PROGRESS', 'WORK_COMPLETED', 'CUSTOMER_CONFIRMED'].includes(row.status) ? (
            <Button
              variant="ghost"
              onClick={async () => {
                try {
                  await marketplaceApi.updateBookingStatus(row.id, 'DISPUTED', 'Customer opened a dispute')
                  await complaintsApi.create({
                    bookingId: row.id,
                    subject: 'Booking dispute',
                    description: 'Customer disputed the booking from the web workspace.',
                  })
                  push('success', 'Dispute opened.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Dispute
            </Button>
          ) : null}
          {row.status === 'WORK_COMPLETED' ? (
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  await marketplaceApi.updateBookingStatus(row.id, 'CUSTOMER_CONFIRMED')
                  push('success', 'Work confirmed.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Confirm completion
            </Button>
          ) : null}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Bookings" description="Confirm a selected quote to book the technician, then use Track job to follow each step until the work is finished." />
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No bookings"
        emptyDescription="Select a quote from a request to start a booking."
        page={page}
        pageSize={10}
        totalCount={total}
        onPageChange={setPage}
      />
      {selected?.address || selected?.latitude != null ? (
        <div className="rounded-2xl border border-black/8 bg-white p-5">
          <LocationMap
            title="Your service location"
            address={selected.address}
            latitude={selected.latitude}
            longitude={selected.longitude}
          />
        </div>
      ) : null}
      {selected ? <BookingTracker booking={selected} history={history} /> : null}
      {scope.length > 0 ? (
        <section className="space-y-3 rounded-2xl border border-black/8 bg-white p-5">
          <h2 className="font-semibold">Scope changes</h2>
          {scope.map((item) => (
            <div key={item.id} className="flex items-center justify-between text-sm">
              <span>
                {item.description} · {item.proposedCost} · {item.customerDecision}
              </span>
              {item.customerDecision === 'PENDING' ? (
                <div className="flex gap-2">
                  <Button
                    variant="secondary"
                    onClick={async () => {
                      await marketplaceApi.decideScopeChange(item.id, 'ACCEPTED')
                      setScope(await marketplaceApi.scopeChanges(item.bookingId))
                    }}
                  >
                    Approve extra work
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={async () => {
                      await marketplaceApi.decideScopeChange(item.id, 'REJECTED')
                      setScope(await marketplaceApi.scopeChanges(item.bookingId))
                    }}
                  >
                    Reject
                  </Button>
                </div>
              ) : null}
            </div>
          ))}
        </section>
      ) : null}
    </div>
  )
}
