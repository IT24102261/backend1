import { useEffect, useMemo, useState } from 'react'
import { reviewsApi } from '../../api/reviews'
import { Button } from '../../components/ui/Button'
import { ConfirmationModal } from '../../components/ui/ConfirmationModal'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FilterPanel, SelectFilter } from '../../components/ui/FilterPanel'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchBar } from '../../components/ui/SearchBar'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { usePagedQuery } from '../../hooks/usePagedQuery'
import { useToastStore } from '../../store/toastStore'
import type { ReviewDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function AdminReviewsPage() {
  const query = usePagedQuery()
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<ReviewDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<ReviewDto | null>(null)
  const [status, setStatus] = useState('HIDDEN')
  const [reason, setReason] = useState('')
  const [rating, setRating] = useState('5')
  const [body, setBody] = useState('')
  const [reply, setReply] = useState('')
  const [removeId, setRemoveId] = useState<string | null>(null)

  function load() {
    setLoading(true)
    reviewsApi
      .adminList(query.params)
      .then((result) => {
        setRows(result.items)
        setTotal(result.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(load, [query.page, query.status])

  function open(row: ReviewDto) {
    setSelected(row)
    setStatus(row.status === 'PUBLISHED' ? 'HIDDEN' : row.status)
    setReason(row.moderationReason ?? '')
    setRating(String(row.rating))
    setBody(row.body ?? '')
    setReply(row.adminReply ?? '')
  }

  const visible = rows.filter((row) => {
    if (!query.search) return true
    const term = query.search.toLowerCase()
    return (
      row.body?.toLowerCase().includes(term) ||
      row.adminReply?.toLowerCase().includes(term) ||
      row.technicianDisplayName?.toLowerCase().includes(term) ||
      row.customerDisplayName?.toLowerCase().includes(term) ||
      row.customerId.toLowerCase().includes(term)
    )
  })

  const columns = useMemo<Column<ReviewDto>[]>(
    () => [
      { key: 'rating', header: 'Rating', render: (row) => `${row.rating} / 5` },
      { key: 'technicianDisplayName', header: 'Technician', render: (row) => row.technicianDisplayName || shortId(row.technicianId) },
      { key: 'body', header: 'Review', render: (row) => row.body || '—' },
      { key: 'adminReply', header: 'Admin reply', render: (row) => row.adminReply || '—' },
      { key: 'bookingId', header: 'Booking', render: (row) => shortId(row.bookingId) },
      { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
      { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
      {
        key: 'action',
        header: 'Action',
        render: (row) => (
          <button type="button" className="font-medium text-[#c4a574] hover:text-[#171717]" onClick={() => open(row)}>
            Manage
          </button>
        ),
      },
    ],
    [],
  )

  return (
    <div className="space-y-5">
      <PageHeader
        title="Reviews"
        description="Reply to customer reviews, edit the published text, hide a review, or delete it."
      />
      <FilterPanel>
        <SearchBar value={query.search} onChange={query.setSearch} placeholder="Search review text" />
        <SelectFilter
          label="Status"
          value={query.status}
          onChange={query.setStatus}
          options={[
            { value: '', label: 'All' },
            { value: 'PUBLISHED', label: 'Published' },
            { value: 'HIDDEN', label: 'Hidden' },
            { value: 'REMOVED', label: 'Removed' },
          ]}
        />
      </FilterPanel>
      {error ? <ErrorState message={error} /> : null}
      <DataTable
        columns={columns}
        rows={visible}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No reviews"
        emptyDescription="No reviews match these filters."
        page={query.page}
        pageSize={query.pageSize}
        totalCount={total}
        onPageChange={query.setPage}
      />
      {selected ? (
        <div className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2">
          <div className="md:col-span-2">
            <h2 className="font-semibold text-[#171717]">Reply to this review</h2>
            <p className="mt-1 text-sm text-[#6d6a64]">
              Booking {shortId(selected.bookingId)}
              {selected.technicianDisplayName ? ` · ${selected.technicianDisplayName}` : ''}
              {selected.customerDisplayName ? ` · ${selected.customerDisplayName}` : ''}
            </p>
            <p className="mt-2 text-sm text-[#6d6a64]">{selected.body || 'No customer comment.'}</p>
            {selected.adminReply ? (
              <p className="mt-3 rounded-xl bg-[#f4efe6] px-3 py-2 text-sm text-[#171717]">
                <span className="font-medium">Current reply:</span> {selected.adminReply}
              </p>
            ) : null}
          </div>
          <FormField label="Rating">
            <TextInput type="number" min="1" max="5" value={rating} onChange={(event) => setRating(event.target.value)} />
          </FormField>
          <FormField label="Status">
            <SelectInput value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="PUBLISHED">Published</option>
              <option value="HIDDEN">Hidden</option>
              <option value="REMOVED">Removed</option>
            </SelectInput>
          </FormField>
          <FormField label="Review text">
            <TextArea value={body} onChange={(event) => setBody(event.target.value)} />
          </FormField>
          <FormField label="Moderation reason">
            <TextArea value={reason} onChange={(event) => setReason(event.target.value)} />
          </FormField>
          <FormField label="Reply to customer">
            <TextArea value={reply} onChange={(event) => setReply(event.target.value)} />
          </FormField>
          <div className="flex flex-wrap items-end gap-2">
            <Button
              onClick={async () => {
                try {
                  await reviewsApi.update(selected.id, Number(rating), body)
                  push('success', 'Review updated.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Save edit
            </Button>
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  const updated = await reviewsApi.reply(selected.id, reply)
                  setSelected(updated)
                  push('success', 'Reply posted.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Post reply
            </Button>
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  await reviewsApi.moderate(selected.id, status, reason)
                  push('success', 'Review moderated.')
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                }
              }}
            >
              Save moderation
            </Button>
            <Button variant="danger" onClick={() => setRemoveId(selected.id)}>
              Delete
            </Button>
          </div>
        </div>
      ) : null}
      <ConfirmationModal
        open={Boolean(removeId)}
        title="Delete review"
        description="This permanently removes the review and recalculates the technician rating."
        confirmLabel="Delete review"
        tone="danger"
        onClose={() => setRemoveId(null)}
        onConfirm={async () => {
          if (!removeId) return
          try {
            await reviewsApi.remove(removeId)
            push('success', 'Review deleted.')
            setRemoveId(null)
            setSelected(null)
            load()
          } catch (err) {
            push('error', getApiError(err).error)
          }
        }}
      />
    </div>
  )
}
