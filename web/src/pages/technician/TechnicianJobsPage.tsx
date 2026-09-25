import { useEffect, useState } from 'react'
import { marketplaceApi } from '../../api/marketplace'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { LocationMap } from '../../components/ui/LocationMap'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { BookingDto } from '../../types/api'
import { formatDate, formatMoney, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

const nextStatus: Record<string, string> = {
  CONFIRMED: 'ACCEPTED',
  ACCEPTED: 'EN_ROUTE',
  EN_ROUTE: 'IN_PROGRESS',
  IN_PROGRESS: 'WORK_COMPLETED',
}

export function TechnicianJobsPage() {
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<BookingDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<BookingDto | null>(null)

  function load() {
    setLoading(true)
    marketplaceApi
      .bookings({ page, pageSize: 10 })
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
        setSelected((current) => result.items.find((item) => item.id === current?.id) ?? result.items[0] ?? current)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(load, [page])

  const columns: Column<BookingDto>[] = [
    { key: 'customerDisplayName', header: 'Customer', render: (row) => row.customerDisplayName || shortId(row.customerId) },
    { key: 'categoryName', header: 'Service', render: (row) => row.categoryName || '—' },
    {
      key: 'requestDescription',
      header: 'Request',
      render: (row) => (
        <button type="button" className="line-clamp-2 max-w-xs text-left font-medium text-[#c4a574] hover:text-[#171717]" onClick={() => setSelected(row)}>
          {row.requestDescription || 'View details'}
        </button>
      ),
    },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    {
      key: 'address',
      header: 'Location',
      render: (row) =>
        row.address || row.latitude != null ? (
          <button type="button" className="text-left font-medium text-[#c4a574] hover:text-[#171717]" onClick={() => setSelected(row)}>
            {row.address || 'View GPS location'}
          </button>
        ) : (
          'Released after confirmation'
        ),
    },
    { key: 'confirmedAt', header: 'Confirmed', render: (row) => formatDate(row.confirmedAt) },
    {
      key: 'action',
      header: 'Update',
      render: (row) =>
        nextStatus[row.status] ? (
          <Button
            variant="secondary"
            onClick={async () => {
              try {
                const updated = await marketplaceApi.updateBookingStatus(row.id, nextStatus[row.status])
                setRows((current) => current.map((item) => (item.id === row.id ? updated : item)))
                setSelected((current) => (current?.id === updated.id ? updated : current))
                push('success', 'Job status updated.')
              } catch (err) {
                push('error', getApiError(err).error)
              }
            }}
          >
            Mark {nextStatus[row.status].replaceAll('_', ' ')}
          </Button>
        ) : (
          '—'
        ),
    },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Jobs" description="Booked jobs show the customer name, request details, and the map pin after confirmation." />
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No jobs"
        emptyDescription="Accepted quotes become jobs after the customer confirms."
        page={page}
        pageSize={10}
        totalCount={total}
        onPageChange={setPage}
      />
      {selected ? (
        <div className="space-y-4 rounded-2xl border border-black/8 bg-white p-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Customer</p>
              <p className="mt-1 text-lg font-semibold text-[#171717]">{selected.customerDisplayName || 'Customer'}</p>
              {selected.customerPhone ? <p className="mt-1 text-sm text-[#6d6a64]">{selected.customerPhone}</p> : null}
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Service</p>
              <p className="mt-1 font-medium text-[#171717]">{selected.categoryName || 'Uncategorised'}</p>
              <p className="mt-1 text-sm text-[#6d6a64]">{selected.serviceArea || 'Area not set'}</p>
            </div>
            <div className="md:col-span-2">
              <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Request details</p>
              <p className="mt-1 text-sm leading-6 text-[#171717]">{selected.requestDescription || 'No description was provided.'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Preferred time</p>
              <p className="mt-1 text-sm text-[#6d6a64]">{formatDate(selected.preferredStart)}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Quote</p>
              <p className="mt-1 text-sm text-[#6d6a64]">
                {selected.quoteTotalAmount != null ? formatMoney(selected.quoteTotalAmount, selected.currency || 'LKR') : '—'}
              </p>
            </div>
          </div>
          {selected.address || selected.latitude != null ? (
            <LocationMap
              title={`Job ${shortId(selected.id)}`}
              address={selected.address}
              latitude={selected.latitude}
              longitude={selected.longitude}
            />
          ) : (
            <p className="text-sm text-[#6d6a64]">Exact address opens after the customer confirms the booking.</p>
          )}
        </div>
      ) : null}
    </div>
  )
}
