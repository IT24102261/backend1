import { useEffect, useState } from 'react'
import { reviewsApi } from '../../api/reviews'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { TableSkeleton } from '../../components/ui/Skeleton'
import { useAuth } from '../../hooks/useAuth'
import type { ReviewDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function TechnicianReviewsPage() {
  const user = useAuth((state) => state.user)
  const [rows, setRows] = useState<ReviewDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!user?.technicianProfileId) {
      setLoading(false)
      return
    }
    reviewsApi
      .forTechnician(user.technicianProfileId)
      .then(setRows)
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [user?.technicianProfileId])

  return (
    <div className="space-y-5">
      <PageHeader
        title="Reviews"
        description="Customer reviews for your completed jobs. Only an admin can reply. You can read them here and in Notifications."
      />
      {loading ? <TableSkeleton /> : null}
      {error ? <ErrorState message={error} /> : null}
      {!loading && !user?.technicianProfileId ? (
        <EmptyState title="Profile required" description="Create a technician profile before reviews can appear." />
      ) : null}
      {!loading && user?.technicianProfileId && rows.length === 0 && !error ? (
        <EmptyState title="No reviews" description="Reviews appear after customers confirm completed work." />
      ) : null}
      <div className="space-y-3">
        {rows.map((row) => (
          <article key={row.id} className="rounded-2xl border border-black/8 bg-white p-4">
            <div className="flex items-center justify-between">
              <p className="font-semibold">{row.rating} / 5</p>
              <StatusBadge status={row.status} />
            </div>
            <p className="mt-1 text-xs text-[#9a968e]">
              Booking {shortId(row.bookingId)}
              {row.customerDisplayName ? ` · ${row.customerDisplayName}` : ''}
            </p>
            <p className="mt-2 text-sm text-slate-600">{row.body || 'No comment'}</p>
            {row.adminReply ? (
              <p className="mt-3 rounded-xl bg-[#f4efe6] px-3 py-2 text-sm text-[#171717]">
                <span className="font-medium">Admin reply:</span> {row.adminReply}
              </p>
            ) : (
              <p className="mt-3 text-xs text-[#9a968e]">Technicians cannot reply. An admin can respond if needed.</p>
            )}
            <p className="mt-2 text-xs text-slate-400">{formatDate(row.createdAt)}</p>
          </article>
        ))}
      </div>
    </div>
  )
}
