import { useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import { adminApi } from '../../api/admin'
import { complaintsApi } from '../../api/complaints'
import { reviewsApi } from '../../api/reviews'
import { techniciansApi } from '../../api/technicians'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { AuditLogDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminAuditPage() {
  const query = usePagedQuery()
  const [rows, setRows] = useState<AuditLogDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [source, setSource] = useState('Derived from operational records')

  useEffect(() => {
    setLoading(true)
    adminApi
      .audit(query.params)
      .then((result) => {
        setRows(result.items)
        setSource('GET /api/admin/audit')
        setError('')
      })
      .catch(async (err) => {
        if (!axios.isAxiosError(err) || err.response?.status !== 404) {
          setError(getApiError(err).error)
          setRows([])
          setLoading(false)
          return
        }
        try {
          const [apps, reviews, complaints] = await Promise.all([
            techniciansApi.adminApplications({ page: 1, pageSize: 50 }),
            reviewsApi.adminList({ page: 1, pageSize: 50 }),
            complaintsApi.adminList({ page: 1, pageSize: 50 }),
          ])
          const derived: AuditLogDto[] = [
            ...apps.items.map((item) => ({
              id: item.id,
              actor: item.decisionNotes ? 'ADMIN' : 'SYSTEM',
              action: item.status,
              entity: 'TechnicianApplication',
              entityId: item.id,
              outcome: item.status === 'APPROVED' ? 'SUCCESS' : item.status,
              timestamp: item.decidedAt ?? item.submittedAt,
            })),
            ...reviews.items.map((item) => ({
              id: item.id,
              actor: 'ADMIN',
              action: 'MODERATE_REVIEW',
              entity: 'Review',
              entityId: item.id,
              outcome: item.status,
              timestamp: item.createdAt,
            })),
            ...complaints.items.map((item) => ({
              id: item.id,
              actor: item.reportedById,
              action: 'COMPLAINT',
              entity: 'Complaint',
              entityId: item.id,
              outcome: item.status,
              timestamp: item.createdAt,
            })),
          ].sort((a, b) => +new Date(b.timestamp) - +new Date(a.timestamp))
          setRows(derived)
          setSource('Derived from applications, reviews, and complaints because /api/admin/audit is not available')
          setError('')
        } catch (inner) {
          setError(getApiError(inner).error)
        }
      })
      .finally(() => setLoading(false))
  }, [query.page])

  const visible = rows.filter((row) => {
    const matchesSearch =
      !query.search ||
      [row.actor, row.action, row.entity, row.entityId].join(' ').toLowerCase().includes(query.search.toLowerCase())
    const matchesStatus = !query.status || row.outcome === query.status
    return matchesSearch && matchesStatus
  })

  const pageRows = visible.slice((query.page - 1) * query.pageSize, query.page * query.pageSize)

  const columns = useMemo<Column<AuditLogDto>[]>(
    () => [
      { key: 'actor', header: 'Actor', render: (row) => row.actor || shortId(row.actorId) },
      { key: 'action', header: 'Action', render: (row) => row.action },
      { key: 'entity', header: 'Entity', render: (row) => row.entity },
      { key: 'entityId', header: 'Entity id', render: (row) => shortId(row.entityId) },
      { key: 'outcome', header: 'Outcome', render: (row) => <StatusBadge status={row.outcome} /> },
      { key: 'timestamp', header: 'Timestamp', render: (row) => formatDate(row.timestamp) },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader title="Audit log" description={source} />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search actor, action, entity" />
        <SelectFilter
          label="Outcome"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'SUCCESS', label: 'Success' },
            { value: 'APPROVED', label: 'Approved' },
            { value: 'REJECTED', label: 'Rejected' },
            { value: 'PUBLISHED', label: 'Published' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={pageRows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No audit events"
        emptyDescription="No operational records are available to display."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={visible.length}
        onPageChange={query.setPage}
      />
    </div>
  )
}
