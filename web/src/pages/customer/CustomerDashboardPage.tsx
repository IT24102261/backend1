import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, Briefcase, ClipboardList, MessageSquareWarning, Plus, Star } from 'lucide-react'
import { marketplaceApi } from '../../api/marketplace'
import { requestsApi } from '../../api/requests'
import { ActivityRow, DashboardSection, QuickAction } from '../../components/ui/DashboardPanel'
import { ErrorState } from '../../components/ui/ErrorState'
import { CardSkeleton } from '../../components/ui/Skeleton'
import { StatCard } from '../../components/ui/StatCard'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { WorkspaceBanner } from '../../components/ui/WorkspaceBanner'
import { useAuth } from '../../hooks/useAuth'
import type { BookingDto, RequestDto } from '../../types/api'
import { formatBookingStatus } from '../../utils/bookingStatus'
import { getApiError } from '../../utils/errors'
import { formatDate, shortId } from '../../utils/format'

export function CustomerDashboardPage() {
  const user = useAuth((state) => state.user)
  const [requests, setRequests] = useState<RequestDto[]>([])
  const [bookings, setBookings] = useState<BookingDto[]>([])
  const [requestCount, setRequestCount] = useState(0)
  const [bookingCount, setBookingCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([requestsApi.list({ page: 1, pageSize: 5 }), marketplaceApi.bookings({ page: 1, pageSize: 5 })])
      .then(([req, book]) => {
        setRequests(req.items)
        setBookings(book.items)
        setRequestCount(req.totalCount)
        setBookingCount(book.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [])

  const waitingConfirm = bookings.filter((item) => item.status === 'PENDING_VALIDATION').length
  const firstName = user?.displayName?.split(' ')[0]

  return (
    <div className="space-y-8">
      <WorkspaceBanner
        kicker="Customer workspace"
        title={`Hello${firstName ? `, ${firstName}` : ''}`}
        description="Create a request, compare quotations, and follow the job until the work is finished."
      />

      {loading ? (
        <div className="grid gap-4 md:grid-cols-3">
          <CardSkeleton />
          <CardSkeleton />
          <CardSkeleton />
        </div>
      ) : null}
      {error ? <ErrorState message={error} /> : null}

      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label="Requests" value={requestCount} hint="Service requests you have submitted" icon={ClipboardList} />
        <StatCard label="Bookings" value={bookingCount} hint={waitingConfirm ? `${waitingConfirm} waiting for your confirmation` : 'Confirmed and active jobs'} icon={Briefcase} accent="navy" />
        <StatCard label="Reviews" value="After completion" hint="Leave a review once you confirm the work" icon={Star} accent="cream" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <QuickAction to="/customer/requests" icon={Plus} title="New request" description="Describe the work and invite eligible technicians." />
        <QuickAction to="/customer/bookings" icon={Briefcase} title="Track a job" description="Follow status, confirm work, and open the map." />
        <QuickAction to="/customer/reviews" icon={Star} title="Leave a review" description="Rate a technician after you confirm completion." />
      </div>

      <div className="grid gap-5 lg:grid-cols-2">
        <DashboardSection
          title="Recent requests"
          description="Your latest service requests."
          action={
            <Link to="/customer/requests" className="inline-flex items-center gap-1 text-sm font-medium text-[#c4a574] hover:text-[#171717]">
              View all <ArrowRight size={14} />
            </Link>
          }
        >
          {requests.length === 0 ? (
            <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No requests yet. Start with a new service request.</p>
          ) : (
            <ul>
              {requests.map((item) => (
                <ActivityRow
                  key={item.id}
                  title={item.categoryName || item.description}
                  meta={`${shortId(item.id)} · ${item.serviceArea || 'Area not set'} · ${formatDate(item.createdAt)}`}
                  badge={<StatusBadge status={item.status} />}
                />
              ))}
            </ul>
          )}
        </DashboardSection>

        <DashboardSection
          title="Recent bookings"
          description="Jobs you have selected or confirmed."
          action={
            <Link to="/customer/bookings" className="inline-flex items-center gap-1 text-sm font-medium text-[#c4a574] hover:text-[#171717]">
              Track jobs <ArrowRight size={14} />
            </Link>
          }
        >
          {bookings.length === 0 ? (
            <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No bookings yet. Select a quotation from a request.</p>
          ) : (
            <ul>
              {bookings.map((item) => (
                <ActivityRow
                  key={item.id}
                  title={item.technicianDisplayName || `Booking ${shortId(item.id)}`}
                  meta={`${item.categoryName || 'Service'} · ${formatBookingStatus(item.status)}`}
                  badge={<StatusBadge status={item.status} label={formatBookingStatus(item.status)} />}
                />
              ))}
            </ul>
          )}
        </DashboardSection>
      </div>

      <DashboardSection title="If something goes wrong" description="Open a complaint against a booked technician. Only an admin can reply.">
        <Link
          to="/customer/complaints"
          className="inline-flex items-center gap-2 rounded-2xl border border-black/8 bg-[#faf7f1] px-4 py-3 text-sm font-medium text-[#171717] transition hover:border-[#c4a574]/50 hover:bg-white"
        >
          <MessageSquareWarning size={16} className="text-[#c4a574]" />
          Go to complaints
        </Link>
      </DashboardSection>
    </div>
  )
}
