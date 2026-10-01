import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, Briefcase, ClipboardList, ScrollText, ShieldCheck, Star } from 'lucide-react'
import { marketplaceApi } from '../../api/marketplace'
import { techniciansApi } from '../../api/technicians'
import { ActivityRow, DashboardSection, QuickAction } from '../../components/ui/DashboardPanel'
import { ErrorState } from '../../components/ui/ErrorState'
import { CardSkeleton } from '../../components/ui/Skeleton'
import { StatCard } from '../../components/ui/StatCard'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
import { WorkspaceBanner } from '../../components/ui/WorkspaceBanner'
import { useAuth } from '../../hooks/useAuth'
import type { BookingDto, InvitationDto, TechnicianApplicationDto, TechnicianProfileDto } from '../../types/api'
import { formatBookingStatus } from '../../utils/bookingStatus'
import { getApiError } from '../../utils/errors'
import { formatDate, shortId } from '../../utils/format'

export function TechnicianDashboardPage() {
  const user = useAuth((state) => state.user)
  const [profile, setProfile] = useState<TechnicianProfileDto | null>(null)
  const [applications, setApplications] = useState<TechnicianApplicationDto[]>([])
  const [invitations, setInvitations] = useState<InvitationDto[]>([])
  const [jobs, setJobs] = useState<BookingDto[]>([])
  const [jobCount, setJobCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([
      techniciansApi.getProfile().catch(() => null),
      techniciansApi.myApplications().catch(() => []),
      marketplaceApi.invitations().catch(() => []),
      marketplaceApi.bookings({ page: 1, pageSize: 50 }).catch(() => ({ items: [], totalCount: 0, page: 1, pageSize: 50, totalPages: 0 })),
    ])
      .then(([nextProfile, apps, invites, bookings]) => {
        setProfile(nextProfile)
        setApplications(apps)
        setInvitations(invites)
        setJobs(bookings.items.slice(0, 5))
        setJobCount(bookings.totalCount)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [])

  const openInvites = invitations.filter((item) => item.status === 'SENT' && item.canQuote !== false)
  const firstName = user?.displayName?.split(' ')[0]
  const approved = profile?.approvedCategories.length ?? 0

  return (
    <div className="space-y-8">
      <WorkspaceBanner
        kicker="Technician workspace"
        title={`Welcome back${firstName ? `, ${firstName}` : ''}`}
        description="Review invitations, send priced quotes, and keep booked jobs moving until the customer confirms."
      />

      {loading ? (
        <div className="grid gap-4 md:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <CardSkeleton key={index} />
          ))}
        </div>
      ) : null}
      {error ? <ErrorState message={error} /> : null}

      {profile ? (
        <Link to="/technician/profile" className="flex items-center gap-4 rounded-2xl border border-black/8 bg-white p-4">
          <TechnicianAvatar name={profile.displayName} photoUrl={profile.profilePhotoUrl} size={64} />
          <span>
            <span className="block text-sm font-medium text-[#171717]">{profile.displayName}</span>
            <span className="block text-sm text-slate-500">Profile photo. Open your profile to change it.</span>
          </span>
        </Link>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Approved trades" value={approved} hint={approved ? 'You can quote in these categories' : 'Apply from Verification'} icon={ShieldCheck} />
        <StatCard label="Open invitations" value={openInvites.length} hint="Requests waiting for your quote" icon={ClipboardList} accent="navy" />
        <StatCard label="Jobs" value={jobCount} hint="Bookings assigned to you" icon={Briefcase} />
        <StatCard label="Rating" value={profile ? Number(profile.averageRating).toFixed(1) : '—'} hint={`${profile?.reviewCount ?? 0} published reviews`} icon={Star} accent="cream" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <QuickAction to="/technician/invitations" icon={ClipboardList} title="Invitations" description="See new requests in your approved trade." />
        <QuickAction to="/technician/quotations" icon={ScrollText} title="Send a quote" description="Price labour, materials, and travel." />
        <QuickAction to="/technician/jobs" icon={Briefcase} title="Active jobs" description="Update status and open the customer map." />
      </div>

      <div className="grid gap-5 lg:grid-cols-2">
        <DashboardSection
          title="Open invitations"
          description="Customers waiting for a quotation."
          action={
            <Link to="/technician/invitations" className="inline-flex items-center gap-1 text-sm font-medium text-[#c4a574] hover:text-[#171717]">
              View all <ArrowRight size={14} />
            </Link>
          }
        >
          {openInvites.length === 0 ? (
            <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No open invitations right now.</p>
          ) : (
            <ul>
              {openInvites.slice(0, 5).map((item) => (
                <ActivityRow
                  key={item.id}
                  title={item.categoryName || 'Service request'}
                  meta={`${item.serviceArea || 'Area not set'} · Preferred ${item.preferredStart ? formatDate(item.preferredStart) : 'time not set'}`}
                  badge={<StatusBadge status={item.status} />}
                />
              ))}
            </ul>
          )}
        </DashboardSection>

        <DashboardSection
          title="Recent jobs"
          description="Bookings that need your update."
          action={
            <Link to="/technician/jobs" className="inline-flex items-center gap-1 text-sm font-medium text-[#c4a574] hover:text-[#171717]">
              Open jobs <ArrowRight size={14} />
            </Link>
          }
        >
          {jobs.length === 0 ? (
            <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No jobs yet. Accepted quotes appear here after the customer books you.</p>
          ) : (
            <ul>
              {jobs.map((item) => (
                <ActivityRow
                  key={item.id}
                  title={item.customerDisplayName || `Job ${shortId(item.id)}`}
                  meta={`${item.categoryName || 'Service'} · ${formatBookingStatus(item.status)}`}
                  badge={<StatusBadge status={item.status} label={formatBookingStatus(item.status)} />}
                />
              ))}
            </ul>
          )}
        </DashboardSection>
      </div>

      <DashboardSection title="Verification status" description="Admin approval for each trade you applied to.">
        {applications.length === 0 ? (
          <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No category applications yet. Apply from the verification page.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {applications.map((item) => (
              <li key={item.id} className="flex items-center justify-between rounded-2xl border border-black/8 bg-[#faf7f1] px-4 py-3">
                <span className="font-medium text-[#171717]">{item.categoryName}</span>
                <StatusBadge status={item.status} />
              </li>
            ))}
          </ul>
        )}
      </DashboardSection>
    </div>
  )
}
