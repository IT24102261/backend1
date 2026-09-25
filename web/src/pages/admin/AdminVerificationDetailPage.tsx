import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { techniciansApi } from '../../api/technicians'
import { Button } from '../../components/ui/Button'
import { ConfirmationModal } from '../../components/ui/ConfirmationModal'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, TextArea } from '../../components/ui/FormField'
import { LoadingSpinner } from '../../components/ui/LoadingSpinner'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { TechnicianApplicationDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

const checklist = [
  { label: 'Identity document provided', source: 'IDENTITY' },
  { label: 'Claimed qualification matches category', source: 'LICENSE / CERTIFICATE' },
  { label: 'Issuer can be verified', source: 'Manual admin check' },
  { label: 'No suspension on technician profile', source: 'Technician profile' },
]

export function AdminVerificationDetailPage() {
  const { id } = useParams()
  const push = useToastStore((state) => state.push)
  const [item, setItem] = useState<TechnicianApplicationDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notes, setNotes] = useState('')
  const [action, setAction] = useState<'approve' | 'reject' | 'request-info' | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!id) return
    techniciansApi
      .adminApplication(id)
      .then((data) => {
        setItem(data)
        setNotes(data.decisionNotes ?? '')
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [id])

  async function confirm() {
    if (!id || !action) return
    setBusy(true)
    try {
      const updated = await techniciansApi.decide(id, action, notes)
      setItem(updated)
      push('success', `Application ${action.replace('-', ' ')} recorded.`)
      setAction(null)
    } catch (err) {
      push('error', getApiError(err).error)
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <LoadingSpinner label="Loading application" />
  if (error) return <ErrorState message={error} />
  if (!item) return <EmptyState title="Not found" description="This application is not available." />

  return (
    <div className="space-y-5">
      <PageHeader
        title="Verification decision"
        description="Approval is category-specific. This decision will not spill into other trades."
        actions={
          <Link to="/admin/verifications" className="text-sm font-medium text-[#c4a574] hover:text-[#171717]">
            Back to list
          </Link>
        }
      />

      <div className="grid gap-4 lg:grid-cols-3">
        <section className="rounded-2xl border border-black/8 bg-white p-5 lg:col-span-2">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-slate-900">Identity information</h2>
            <StatusBadge status={item.status} />
          </div>
          <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
            <div>
              <dt className="text-slate-400">Technician</dt>
              <dd className="font-medium">{item.technicianId}</dd>
            </div>
            <div>
              <dt className="text-slate-400">Claimed qualification</dt>
              <dd className="font-medium">{item.categoryName || '—'}</dd>
            </div>
            <div>
              <dt className="text-slate-400">Issuer</dt>
              <dd>Not returned by the application API</dd>
            </div>
            <div>
              <dt className="text-slate-400">Certificate number</dt>
              <dd>Not returned by the application API</dd>
            </div>
            <div>
              <dt className="text-slate-400">Submitted</dt>
              <dd>{formatDate(item.submittedAt)}</dd>
            </div>
            <div>
              <dt className="text-slate-400">Checked date</dt>
              <dd>{formatDate(item.decidedAt)}</dd>
            </div>
          </dl>
        </section>
        <section className="rounded-2xl border border-black/8 bg-white p-5">
          <h2 className="font-semibold text-slate-900">Admin notes</h2>
          <FormField label="Decision notes">
            <TextArea value={notes} onChange={(event) => setNotes(event.target.value)} />
          </FormField>
          <div className="mt-4 flex flex-col gap-2">
            <Button onClick={() => setAction('approve')}>Approve</Button>
            <Button variant="secondary" onClick={() => setAction('request-info')}>
              Request More Information
            </Button>
            <Button variant="danger" onClick={() => setAction('reject')}>
              Reject
            </Button>
          </div>
        </section>
      </div>

      <section className="rounded-2xl border border-black/8 bg-white p-5">
        <h2 className="font-semibold text-slate-900">Documents</h2>
        <p className="mt-1 text-sm text-slate-500">
          Document metadata is uploaded by the technician; this admin DTO does not include the file list.
        </p>
        <div className="mt-4">
          <EmptyState
            title="No documents in this payload"
            description={`Application ${shortId(item.id)} has no document collection on GET /api/admin/technician-applications/{id}.`}
          />
        </div>
      </section>

      <section className="rounded-2xl border border-black/8 bg-white p-5">
        <h2 className="font-semibold text-slate-900">Verification checklist</h2>
        <ul className="mt-4 space-y-3">
          {checklist.map((row) => (
            <li key={row.label} className="flex items-start justify-between gap-4 bg-[#f4efe6] px-4 py-3 text-sm">
              <div>
                <p className="font-medium text-slate-800">{row.label}</p>
                <p className="text-slate-500">Source: {row.source}</p>
              </div>
              <span className="text-xs text-slate-400">Manual</span>
            </li>
          ))}
        </ul>
      </section>

      <ConfirmationModal
        open={action !== null}
        title="Confirm verification decision"
        description="This writes the category application status on the server. It does not approve any other trade."
        confirmLabel={action === 'reject' ? 'Reject application' : 'Confirm'}
        tone={action === 'reject' ? 'danger' : 'default'}
        busy={busy}
        onClose={() => setAction(null)}
        onConfirm={() => void confirm()}
      />
    </div>
  )
}
