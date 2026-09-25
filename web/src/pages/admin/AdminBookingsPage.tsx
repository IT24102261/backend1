import { useEffect, useMemo, useState } from 'react'
import { marketplaceApi } from '../../api/marketplace'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { BookingDto, QuoteDto } from '../../types/api'
import { formatDate, formatMoney, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminBookingsPage() {
  const query = usePagedQuery()
  const [rows, setRows] = useState<BookingDto[]>([])
  const [quotes, setQuotes] = useState<Record<string, QuoteDto>>({})
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    marketplaceApi
      .bookings(query.params)
      .then(async (result) => {
        setRows(result.items)
        setTotal(result.totalCount)
        const entries = await Promise.all(
          result.items.map(async (booking) => {
            try {
              return [booking.quotationId, await marketplaceApi.quote(booking.quotationId)] as const
            } catch {
              return [booking.quotationId, null] as const
            }
          }),
        )
        setQuotes(
          Object.fromEntries(entries.filter((entry): entry is readonly [string, QuoteDto] => entry[1] !== null)),
        )
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [query.page, query.search, query.status])

  const visible = rows.filter((row) => {
    if (!query.search) return true
    const term = query.search.toLowerCase()
    return row.customerId.toLowerCase().includes(term) || row.technicianId.toLowerCase().includes(term)
  })

  const columns = useMemo<Column<BookingDto>[]>(
    () => [
      { key: 'customerId', header: 'Customer', render: (row) => shortId(row.customerId) },
      { key: 'technicianId', header: 'Technician', render: (row) => shortId(row.technicianId) },
      {
        key: 'amount',
        header: 'Quote amount',
        render: (row) => {
          const quote = quotes[row.quotationId]
          return quote ? formatMoney(quote.totalAmount, quote.currency) : '—'
        },
      },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      {
        key: 'timeline',
        header: 'Booking timeline',
        render: (row) => (
          <div className="text-xs text-slate-500">
            <p>Approved {formatDate(row.approvedAt)}</p>
            <p>Confirmed {formatDate(row.confirmedAt)}</p>
            <p>Address released {formatDate(row.addressReleaseAt)}</p>
          </div>
        ),
      },
    ],
    [quotes],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Booking management" description="Search bookings by status, customer, technician, and quote." />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Customer or technician id" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'PENDING_VALIDATION', label: 'Pending validation' },
            { value: 'CONFIRMED', label: 'Confirmed' },
            { value: 'IN_PROGRESS', label: 'In progress' },
            { value: 'CUSTOMER_CONFIRMED', label: 'Customer confirmed' },
            { value: 'CLOSED', label: 'Closed' },
            { value: 'CANCELLED', label: 'Cancelled' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={visible}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No bookings"
        emptyDescription="No bookings match these filters."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        onPageChange={query.setPage}
      />
    </div>
  )
}
