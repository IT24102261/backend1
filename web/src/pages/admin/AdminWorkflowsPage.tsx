import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../../api/admin'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import type { AgentReportDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminWorkflowsPage() {
  const query = usePagedQuery()
  const [rows, setRows] = useState<AgentReportDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    adminApi
      .agentReports(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [query.page, query.status])

  const visible = rows.filter((row) => {
    if (!query.search) return true
    return row.requestId.toLowerCase().includes(query.search.toLowerCase()) || row.id.toLowerCase().includes(query.search.toLowerCase())
  })

  const columns = useMemo<Column<AgentReportDto>[]>(
    () => [
      { key: 'id', header: 'Workflow ID', render: (row) => shortId(row.id) },
      { key: 'requestId', header: 'Request', render: (row) => shortId(row.requestId) },
      { key: 'objective', header: 'Objective', render: () => 'See workflow detail' },
      { key: 'currentStep', header: 'Current Step', render: (row) => row.status },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      { key: 'approvalStatus', header: 'Approval Status', render: (row) => <StatusBadge status={row.approvalStatus} /> },
      { key: 'startedAt', header: 'Started', render: (row) => formatDate(row.startedAt) },
      { key: 'duration', header: 'Duration', render: () => '—' },
      {
        key: 'open',
        header: '',
        render: (row) => (
          <Link to={`/admin/ai-workflows/${row.id}`} className="font-medium text-[#c4a574] hover:text-[#171717]">
            Open
          </Link>
        ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader
        title="Agent workflow monitor"
        description="Only structured summaries from the API are shown. Hidden chain-of-thought is never requested or rendered."
      />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Workflow or request id" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'PLANNING', label: 'Planning' },
            { value: 'MATCHING', label: 'Matching' },
            { value: 'QUOTE_COLLECTION', label: 'Quote collection' },
            { value: 'WAITING_FOR_CUSTOMER_APPROVAL', label: 'Waiting approval' },
            { value: 'COMPLETED', label: 'Completed' },
            { value: 'FAILED', label: 'Failed' },
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
        emptyTitle="No workflows"
        emptyDescription="No AI workflows have been recorded yet."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        onPageChange={query.setPage}
      />
    </div>
  )
}
