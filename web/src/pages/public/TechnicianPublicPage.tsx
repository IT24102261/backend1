import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { reviewsApi } from '../../api/reviews'
import { techniciansApi } from '../../api/technicians'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { StarRating } from '../../components/ui/StarRating'
import { TableSkeleton } from '../../components/ui/Skeleton'
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
import type { PublicTechnicianDto, ReviewDto } from '../../types/api'
import { formatDate } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function TechnicianPublicPage() {
  const { id } = useParams()
  const [profile, setProfile] = useState<PublicTechnicianDto | null>(null)
  const [reviews, setReviews] = useState<ReviewDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return
    Promise.all([techniciansApi.publicProfile(id), reviewsApi.forTechnician(id)])
      .then(([publicProfile, items]) => {
        setProfile(publicProfile)
        setReviews(items)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [id])

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <PageHeader
        title={profile?.displayName ?? 'Technician'}
        description="Public marketplace profile only. Qualification files and identity evidence are not shown."
      />
      {loading ? <TableSkeleton /> : null}
      {error ? <ErrorState message={error} /> : null}
      {profile ? (
        <div className="mt-6 flex items-center gap-4">
          <TechnicianAvatar name={profile.displayName} photoUrl={profile.profilePhotoUrl} size={72} />
          <p className="text-sm text-slate-600">Customers see this photo on quotations and when they confirm a booking.</p>
        </div>
      ) : null}
      {profile ? (
        <dl className="mt-6 grid gap-3 rounded-2xl border border-black/8 bg-white p-5 text-sm md:grid-cols-2">
          <div>
            <dt className="text-slate-400">Approved categories</dt>
            <dd className="font-medium">{profile.approvedCategories.join(', ') || 'None yet'}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Verification badge</dt>
            <dd>{profile.categoryVerified ? <StatusBadge status="APPROVED" /> : <StatusBadge status="PENDING" />}</dd>
          </div>
          <div>
            <dt className="text-slate-400">Average rating</dt>
            <dd className="mt-1">
              <StarRating value={Number(profile.averageRating)} count={profile.reviewCount} />
            </dd>
          </div>
          <div>
            <dt className="text-slate-400">Verified reviews / completed jobs</dt>
            <dd className="font-medium">
              {profile.reviewCount} / {profile.completedJobs}
            </dd>
          </div>
          <div className="md:col-span-2">
            <dt className="text-slate-400">Service summary</dt>
            <dd>{profile.serviceSummary || '—'}</dd>
          </div>
        </dl>
      ) : null}
      <div className="mt-6 space-y-3">
        {!loading && !error && reviews.length === 0 ? (
          <EmptyState title="No reviews yet" description="This technician does not have published reviews." />
        ) : null}
        {reviews.map((review) => (
          <article key={review.id} className="rounded-2xl border border-black/8 bg-white p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="font-semibold text-slate-900">{review.rating} / 5</p>
              <StatusBadge status={review.status} />
            </div>
            <p className="mt-2 text-sm text-slate-600">{review.body || 'No written comments.'}</p>
            {review.adminReply ? (
              <p className="mt-3 rounded-xl bg-[#f4efe6] px-3 py-2 text-sm text-[#171717]">
                <span className="font-medium">Admin reply:</span> {review.adminReply}
              </p>
            ) : null}
            <p className="mt-2 text-xs text-slate-400">{formatDate(review.createdAt)}</p>
          </article>
        ))}
      </div>
    </div>
  )
}
