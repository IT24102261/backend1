import { useEffect, useMemo, useState } from 'react'
import { requestsApi } from '../../api/requests'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { RequestDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

function classification(status: string) {
  if (status === 'ANALYZING' || status === 'MATCHING' || status === 'COLLECTING_QUOTES') return 'RUNNING'
  if (status === 'CLARIFICATION_REQUIRED') return 'WAITING_APPROVAL'
  if (status === 'FAILED') return 'FAILED'
  if (status === 'DRAFT') return 'PENDING'
  return 'COMPLETED'
}

export function AdminRequestsPage() {
  const query = usePagedQuery()
  const [rows, setRows] = useState<RequestDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    requestsApi
      .list(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [query.page, query.search, query.status, query.sortBy, query.sortDir])

  const columns = useMemo<Column<RequestDto>[]>(
    () => [
      { key: 'customerId', header: 'Customer', render: (row) => shortId(row.customerId) },
      { key: 'categoryName', header: 'Category', render: (row) => row.categoryName || '—' },
      { key: 'serviceArea', header: 'Service area', render: (row) => row.serviceArea || '—' },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      { key: 'createdAt', header: 'Created date', sortable: true, render: (row) => formatDate(row.createdAt) },
      {
        key: 'ai',
        header: 'AI classification status',
        render: (row) => <StatusBadge status={classification(row.status)} />,
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Request management" description="Customer requests with category, area, and AI-related status." />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search description or area" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'DRAFT', label: 'Draft' },
            { value: 'SUBMITTED', label: 'Submitted' },
            { value: 'ANALYZING', label: 'Analyzing' },
            { value: 'MATCHING', label: 'Matching' },
            { value: 'COLLECTING_QUOTES', label: 'Collecting quotes' },
            { value: 'BOOKED', label: 'Booked' },
            { value: 'COMPLETED', label: 'Completed' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No requests"
        emptyDescription="No service requests match these filters."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        sortBy={query.sortBy}
        sortDir={query.sortDir}
        onSort={query.toggleSort}
        onPageChange={query.setPage}
      />
    </div>
  )
}
