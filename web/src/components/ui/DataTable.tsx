import type { ReactNode } from 'react'
import { EmptyState } from './EmptyState'
import { Pagination } from './Pagination'
import { TableSkeleton } from './Skeleton'

export type Column<T> = {
  key: string
  header: string
  sortable?: boolean
  className?: string
  render: (row: T) => ReactNode
}

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading,
  emptyTitle,
  emptyDescription,
  page,
  pageSize,
  totalCount,
  sortBy,
  sortDir,
  onSort,
  onPageChange,
}: {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  emptyTitle?: string
  emptyDescription?: string
  page: number
  pageSize: number
  totalCount: number
  sortBy?: string
  sortDir?: 'asc' | 'desc'
  onSort?: (key: string) => void
  onPageChange: (page: number) => void
}) {
  return (
    <div className="overflow-hidden rounded-2xl border border-black/8 bg-white shadow-[var(--shadow-card)]">
      <div className="overflow-x-auto">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-[#f4efe6] text-xs font-semibold text-[#6d6a64]">
            <tr>
              {columns.map((column) => (
                <th key={column.key} className={`px-4 py-3.5 font-semibold ${column.className ?? ''}`}>
                  {column.sortable && onSort ? (
                    <button
                      type="button"
                      className="inline-flex items-center gap-1 hover:text-[#171717]"
                      onClick={() => onSort(column.key)}
                    >
                      {column.header}
                      {sortBy === column.key ? <span>{sortDir === 'asc' ? '↑' : '↓'}</span> : null}
                    </button>
                  ) : (
                    column.header
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-black/5">
            {loading ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-6">
                  <TableSkeleton rows={5} />
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-6">
                  <EmptyState
                    title={emptyTitle ?? 'No records'}
                    description={emptyDescription ?? 'Nothing matches the current filters.'}
                  />
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={rowKey(row)} className="transition hover:bg-[#f4efe6]/70">
                  {columns.map((column) => (
                    <td key={column.key} className={`px-4 py-3.5 text-[#3f3c38] ${column.className ?? ''}`}>
                      {column.render(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={onPageChange} />
    </div>
  )
}
