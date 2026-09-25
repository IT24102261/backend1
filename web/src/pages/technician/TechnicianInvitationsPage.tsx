import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowUpRight, MapPin } from 'lucide-react'
import { marketplaceApi } from '../../api/marketplace'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { TableSkeleton } from '../../components/ui/Skeleton'
import { useToastStore } from '../../store/toastStore'
import type { InvitationDto } from '../../types/api'
import { formatDate } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function TechnicianInvitationsPage() {
  const push = useToastStore((state) => state.push)
  const [rows, setRows] = useState<InvitationDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    marketplaceApi
      .invitations()
      .then(setRows)
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [])

  async function decide(id: string, action: 'accept' | 'decline') {
    try {
      const updated = action === 'accept' ? await marketplaceApi.accept(id) : await marketplaceApi.decline(id)
      setRows((current) => current.map((item) => (item.id === id ? updated : item)))
      push('success', `Invitation ${action}ed.`)
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Invitations"
        description="Exact customer addresses are withheld until a booking is confirmed."
      />
      {loading ? <TableSkeleton /> : null}
      {error ? <ErrorState message={error} /> : null}
      {!loading && !error && rows.length === 0 ? (
        <EmptyState title="No invitations" description="You will see jobs here after a matching request is submitted." />
      ) : null}
      <div className="grid gap-4">
        {rows.map((item) => (
          <article
            key={item.id}
            className="rounded-2xl border border-black/8 bg-white p-5 shadow-[var(--shadow-card)] transition hover:shadow-[var(--shadow-card-hover)]"
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-lg font-semibold text-slate-900">{item.categoryName || 'Service request'}</p>
                <p className="mt-1 text-sm leading-6 text-slate-600">{item.description}</p>
                <p className="mt-3 inline-flex items-center gap-1.5 text-xs text-slate-400">
                  <MapPin size={13} />
                  {item.serviceArea || 'approximate'} · {formatDate(item.sentAt)}
                </p>
              </div>
              <StatusBadge status={item.status} />
            </div>
            <div className="mt-5 flex flex-wrap items-center gap-2">
              {item.status === 'SENT' ? (
                <>
                  <Button onClick={() => void decide(item.id, 'accept')}>Accept</Button>
                  <Button variant="secondary" onClick={() => void decide(item.id, 'decline')}>
                    Decline
                  </Button>
                </>
              ) : null}
              {item.status !== 'DECLINED' ? (
                <Link
                  to={`/technician/quotations?invitationId=${item.id}`}
                  className="inline-flex items-center gap-1 px-3 py-2 text-sm font-semibold text-[#c4a574] transition hover:bg-[#f4efe6]"
                >
                  Create quotation
                  <ArrowUpRight size={14} />
                </Link>
              ) : null}
            </div>
          </article>
        ))}
      </div>
    </div>
  )
}
