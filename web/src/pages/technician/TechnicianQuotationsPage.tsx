import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowUpRight, Receipt } from 'lucide-react'
import { marketplaceApi } from '../../api/marketplace'
import { Button } from '../../components/ui/Button'
import { Card, SectionLabel } from '../../components/ui/Card'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { InvitationDto, QuoteDto } from '../../types/api'
import { formatMoney } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function TechnicianQuotationsPage() {
  const push = useToastStore((state) => state.push)
  const [searchParams] = useSearchParams()
  const [invitations, setInvitations] = useState<InvitationDto[]>([])
  const [quotes, setQuotes] = useState<QuoteDto[]>([])
  const [invitationId, setInvitationId] = useState(searchParams.get('invitationId') ?? '')
  const [loadingInvites, setLoadingInvites] = useState(true)
  const [labour, setLabour] = useState('0')
  const [materials, setMaterials] = useState('0')
  const [travel, setTravel] = useState('0')
  const [arrivalStart, setArrivalStart] = useState('')
  const [assumptions, setAssumptions] = useState('')
  const [included, setIncluded] = useState('')
  const [excluded, setExcluded] = useState('')
  const [error, setError] = useState('')
  const [validation, setValidation] = useState('')

  const quotable = useMemo(
    () => invitations.filter((item) => item.status !== 'DECLINED'),
    [invitations],
  )
  const selected = quotable.find((item) => item.id === invitationId)

  useEffect(() => {
    marketplaceApi
      .invitations()
      .then(async (items) => {
        const list = Array.isArray(items) ? items : []
        setInvitations(list)
        const preferred = searchParams.get('invitationId')
        const open = list.filter((item) => item.status !== 'DECLINED')
        if (preferred && open.some((item) => item.id === preferred)) {
          setInvitationId(preferred)
        } else if (open.length === 1) {
          setInvitationId(open[0].id)
        }
        const collected: QuoteDto[] = []
        for (const item of list) {
          try {
            collected.push(...(await marketplaceApi.requestQuotes(item.requestId)))
          } catch {
            // A technician may not see every request's quotes.
          }
        }
        setQuotes(collected)
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoadingInvites(false))
  }, [searchParams])

  const total = Number(labour) + Number(materials) + Number(travel)

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!invitationId) {
      setValidation('Select an invitation.')
      return
    }
    setValidation('')
    try {
      const quote = await marketplaceApi.createQuote(invitationId, {
        labourAmount: Number(labour),
        materialsAmount: Number(materials),
        travelAmount: Number(travel),
        totalAmount: total,
        currency: 'LKR',
        arrivalStart: arrivalStart ? new Date(arrivalStart).toISOString() : undefined,
        assumptions,
        includedMaterials: included || undefined,
        excludedMaterials: excluded || undefined,
      })
      setQuotes((current) => [quote, ...current])
      push('success', 'Quote submitted. Revisions create a new version.')
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Quotations"
        description="Total must equal labour + materials + travel. Submitted quotes are immutable."
      />
      {error ? <ErrorState message={error} /> : null}

      <form className="space-y-5" onSubmit={submit}>
        <Card className="p-6">
          <SectionLabel>Job to quote</SectionLabel>
          <div className="mt-4 grid gap-5 md:grid-cols-2">
            <FormField
              label="Invitation"
              hint={
                loadingInvites
                  ? 'Loading invitations…'
                  : quotable.length === 0
                    ? 'No jobs to quote yet. Accept an invitation first.'
                    : `${quotable.length} invitation${quotable.length === 1 ? '' : 's'} available`
              }
            >
              <SelectInput
                value={invitationId}
                disabled={loadingInvites || quotable.length === 0}
                onChange={(event) => setInvitationId(event.target.value)}
              >
                <option value="">{loadingInvites ? 'Loading invitations…' : 'Select invitation'}</option>
                {quotable.map((item) => (
                  <option key={item.id} value={item.id}>
                    {(item.categoryName || 'Request') +
                      ' · ' +
                      (item.serviceArea || 'Area not set') +
                      ' · ' +
                      (item.description || 'No description').slice(0, 40) +
                      ' · ' +
                      item.status}
                  </option>
                ))}
              </SelectInput>
              {quotable.length === 0 && !loadingInvites ? (
                <p className="mt-2 text-sm">
                  <Link to="/technician/invitations" className="inline-flex items-center gap-1 font-semibold text-[#c4a574] hover:text-[#171717]">
                    Open invitations
                    <ArrowUpRight size={14} />
                  </Link>
                </p>
              ) : null}
            </FormField>
            {selected ? (
              <div className="border border-[#e6dccb] bg-[#f4efe6] p-4">
                <p className="text-sm font-semibold text-slate-900">{selected.categoryName || 'Service request'}</p>
                <p className="mt-1 text-sm leading-6 text-slate-600">{selected.description || 'No description'}</p>
                <p className="mt-2 text-xs text-slate-500">{selected.serviceArea || 'Area not set'}</p>
              </div>
            ) : (
              <div className="border border-dashed border-[#e6dccb] bg-[#f4efe6] p-4 text-sm text-[#6d6a64]">
                Choose an invitation to see the job summary here.
              </div>
            )}
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <SectionLabel>Pricing</SectionLabel>
            <div className="rounded-2xl bg-slate-900 px-4 py-3 text-white shadow-sm">
              <p className="text-[11px] tracking-wide text-slate-300 uppercase">Total</p>
              <p className="text-xl font-semibold">{formatMoney(total)}</p>
            </div>
          </div>
          <div className="mt-5 grid gap-5 md:grid-cols-3">
            <FormField label="Labour">
              <TextInput type="number" min="0" step="0.01" value={labour} onChange={(event) => setLabour(event.target.value)} />
            </FormField>
            <FormField label="Materials">
              <TextInput type="number" min="0" step="0.01" value={materials} onChange={(event) => setMaterials(event.target.value)} />
            </FormField>
            <FormField label="Travel">
              <TextInput type="number" min="0" step="0.01" value={travel} onChange={(event) => setTravel(event.target.value)} />
            </FormField>
          </div>
        </Card>

        <Card className="p-6">
          <SectionLabel>Quote details</SectionLabel>
          <div className="mt-5 grid gap-5 md:grid-cols-2">
            <FormField label="Proposed arrival">
              <TextInput type="datetime-local" value={arrivalStart} onChange={(event) => setArrivalStart(event.target.value)} />
            </FormField>
            <FormField label="Assumptions">
              <TextArea value={assumptions} onChange={(event) => setAssumptions(event.target.value)} />
            </FormField>
            <div className="space-y-5">
              <FormField label="Included materials">
                <TextInput value={included} onChange={(event) => setIncluded(event.target.value)} />
              </FormField>
              <FormField label="Excluded materials">
                <TextInput value={excluded} onChange={(event) => setExcluded(event.target.value)} />
              </FormField>
            </div>
          </div>
          {validation ? <p className="mt-4 text-sm font-medium text-rose-600">{validation}</p> : null}
          <div className="mt-6">
            <Button type="submit">Submit quote</Button>
          </div>
        </Card>
      </form>

      <section className="space-y-3">
        <div className="flex items-center gap-2">
          <Receipt size={16} className="text-[#c4a574]" />
          <h2 className="text-sm font-semibold text-slate-900">Submitted quotes</h2>
        </div>
        {quotes.length === 0 ? (
          <EmptyState title="No quotes yet" description="Accepted invitations can receive a priced quotation." />
        ) : (
          <div className="grid gap-3">
            {quotes.map((quote) => (
              <article
                key={quote.id}
                className="flex items-center justify-between rounded-2xl border border-black/8 bg-white p-4 shadow-[var(--shadow-card)]"
              >
                <div>
                  <p className="text-lg font-semibold text-slate-900">{formatMoney(quote.totalAmount, quote.currency)}</p>
                  <p className="mt-1 text-xs text-slate-500">Version {quote.version}</p>
                </div>
                <StatusBadge status={quote.status} />
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
