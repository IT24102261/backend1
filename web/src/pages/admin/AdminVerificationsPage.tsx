import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { techniciansApi } from '../../api/technicians'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { TechnicianApplicationDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminVerificationsPage() {
  const query = usePagedQuery()
  const [rows, setRows] = useState<TechnicianApplicationDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    techniciansApi
      .adminApplications(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [query.page, query.search, query.status, query.sortBy, query.sortDir])

  const columns = useMemo<Column<TechnicianApplicationDto>[]>(
    () => [
      { key: 'technicianId', header: 'Technician', sortable: true, render: (row) => shortId(row.technicianId) },
      { key: 'categoryName', header: 'Category', sortable: true, render: (row) => row.categoryName || '—' },
      { key: 'submittedAt', header: 'Submitted Date', sortable: true, render: (row) => formatDate(row.submittedAt) },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      { key: 'evidence', header: 'Evidence Count', render: () => '—' },
      {
        key: 'action',
        header: 'Action',
        render: (row) => (
          <Link to={`/admin/verifications/${row.id}`} className="font-medium text-[#c4a574] hover:text-[#171717]">
            Review
          </Link>
        ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader
        title="Technician verification"
        description="Category applications only. Approving plumber status never grants electrician status."
      />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search applications" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All statuses' },
            { value: 'SUBMITTED', label: 'Submitted' },
            { value: 'MORE_INFORMATION_REQUIRED', label: 'More information' },
            { value: 'APPROVED', label: 'Approved' },
            { value: 'REJECTED', label: 'Rejected' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No applications"
        emptyDescription="No technician applications match these filters."
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
