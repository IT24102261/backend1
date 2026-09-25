import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../../api/admin'
import { techniciansApi } from '../../api/technicians'
import { Button } from '../../components/ui/Button'
import { useToastStore } from '../../store/toastStore'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { TechnicianReportDto } from '../../types/api'
import { getApiError } from '../../utils/errors'

export function AdminTechniciansPage() {
  const push = useToastStore((state) => state.push)
  const query = usePagedQuery()
  const [rows, setRows] = useState<TechnicianReportDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    adminApi
      .technicianReports(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [query.page, query.search, query.status, query.sortBy, query.sortDir])

  const filtered = rows.filter((row) => {
    if (query.status === 'SUSPENDED') return row.isSuspended
    if (query.status === 'ACTIVE') return !row.isSuspended
    return true
  })

  const columns = useMemo<Column<TechnicianReportDto>[]>(
    () => [
      { key: 'displayName', header: 'Technician', sortable: true, render: (row) => row.displayName },
      { key: 'averageRating', header: 'Rating', sortable: true, render: (row) => Number(row.averageRating).toFixed(1) },
      { key: 'reviewCount', header: 'Reviews', render: (row) => row.reviewCount },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge status={row.isSuspended ? 'SUSPENDED' : 'ACTIVE'} />,
      },
      {
        key: 'approvals',
        header: 'Category approvals',
        render: (row) => (
          <div className="flex flex-wrap gap-2">
            <Link to={`/technicians/${row.id}`} className="font-medium text-[#c4a574] hover:text-[#171717]">
              Public profile
            </Link>
            <Button
              variant="ghost"
              onClick={async () => {
                try {
                  if (row.isSuspended) await techniciansApi.reactivate(row.id, 'Reactivated by admin')
                  else await techniciansApi.suspend(row.id, 'Suspended by admin')
                  push('success', 'Technician status updated.')
                  query.setPage(1)
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              {row.isSuspended ? 'Reactivate' : 'Suspend'}
            </Button>
          </div>
        ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Technician management" description="Search and review technician rating, suspension, and public reviews." />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search technicians" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'ACTIVE', label: 'Active' },
            { value: 'SUSPENDED', label: 'Suspended' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={filtered}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No technicians"
        emptyDescription="No technician reports match these filters."
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
