import { useEffect, useMemo, useRef, useState } from 'react'
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
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
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
  const [uploadingId, setUploadingId] = useState('')
  const fileInputs = useRef<Record<string, HTMLInputElement | null>>({})

  function load() {
    setLoading(true)
    adminApi
      .technicianReports(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
  }, [query.page, query.search, query.status, query.sortBy, query.sortDir])

  const filtered = rows.filter((row) => {
    if (query.status === 'SUSPENDED') return row.isSuspended
    if (query.status === 'ACTIVE') return !row.isSuspended
    return true
  })

  async function uploadPhoto(id: string, file?: File) {
    if (!file) return
    setUploadingId(id)
    try {
      const updated = await techniciansApi.setProfilePhoto(id, file)
      setRows((current) =>
        current.map((row) => (row.id === id ? { ...row, profilePhotoUrl: updated.profilePhotoUrl } : row)),
      )
      push('success', 'Profile photo saved. Customers will see it on quotations and booking confirm.')
    } catch (err) {
      push('error', getApiError(err).error)
    } finally {
      setUploadingId('')
    }
  }

  const columns = useMemo<Column<TechnicianReportDto>[]>(
    () => [
      {
        key: 'displayName',
        header: 'Technician',
        sortable: true,
        render: (row) => (
          <span className="inline-flex items-center gap-3">
            <TechnicianAvatar name={row.displayName} photoUrl={row.profilePhotoUrl} size={40} />
            {row.displayName}
          </span>
        ),
      },
      { key: 'averageRating', header: 'Rating', sortable: true, render: (row) => Number(row.averageRating).toFixed(1) },
      { key: 'reviewCount', header: 'Reviews', render: (row) => row.reviewCount },
      {
        key: 'status',
        header: 'Status',
        render: (row) => <StatusBadge status={row.isSuspended ? 'SUSPENDED' : 'ACTIVE'} />,
      },
      {
        key: 'approvals',
        header: 'Actions',
        render: (row) => (
          <div className="flex flex-wrap items-center gap-2">
            <input
              ref={(element) => {
                fileInputs.current[row.id] = element
              }}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={(event) => {
                void uploadPhoto(row.id, event.target.files?.[0])
                event.target.value = ''
              }}
            />
            <Button
              variant="secondary"
              disabled={uploadingId === row.id}
              onClick={() => fileInputs.current[row.id]?.click()}
            >
              {uploadingId === row.id ? 'Saving…' : 'Set photo'}
            </Button>
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
    [uploadingId],
  )

  return (
    <div className="space-y-5">
      <PageHeader
        title="Technician management"
        description="Add or replace a profile photo for each technician. Customers see this picture on quotations and when they confirm a booking."
      />
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
