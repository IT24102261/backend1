import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { categoriesApi } from '../../api/categories'
import { mapsApi } from '../../api/maps'
import { marketplaceApi } from '../../api/marketplace'
import { requestsApi } from '../../api/requests'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
import { useToastStore } from '../../store/toastStore'
import type { CategoryDto, QuoteDto, RequestDto } from '../../types/api'
import { formatDate, formatMoney } from '../../utils/format'
import { getApiError } from '../../utils/errors'
import { datetimeLocalMin, isFutureDateTime } from '../../utils/validation'

function requestHelpText(status: string) {
  switch (status) {
    case 'DRAFT':
      return 'This request is still a draft. Submit it so we can understand the job and find technicians.'
    case 'ANALYZING':
    case 'SUBMITTED':
      return 'We are reading your request to identify the right trade, such as electrician or plumber.'
    case 'CLARIFICATION_REQUIRED':
      return 'We need a little more information from you before we can invite technicians.'
    case 'MATCHING':
      return 'We are inviting technicians who are approved for this type of work.'
    case 'COLLECTING_QUOTES':
      return 'Technicians can send quotations. When they arrive, we will explain them so you can compare.'
    case 'AWAITING_CUSTOMER_APPROVAL':
      return 'Quotations are ready. Pick one below, then confirm the booking. We will not book for you.'
    case 'BOOKED':
    case 'COMPLETED':
      return 'A quotation was selected. Follow the job on Bookings.'
    case 'FAILED':
      return 'We could not finish matching this request. You can continue from the list above.'
    case 'CANCELLED':
      return 'You cancelled this request. Technicians who were invited or who sent quotations were notified.'
    default:
      return 'Follow this request as technicians reply. You always choose the quotation.'
  }
}

export function CustomerRequestsPage() {
  const push = useToastStore((state) => state.push)
  const navigate = useNavigate()
  const [rows, setRows] = useState<RequestDto[]>([])
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [quotesByRequest, setQuotesByRequest] = useState<Record<string, QuoteDto[]>>({})
  const [selectedRequestId, setSelectedRequestId] = useState('')
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [description, setDescription] = useState('')
  const [serviceArea, setServiceArea] = useState('')
  const [address, setAddress] = useState('')
  const [latitude, setLatitude] = useState<number | null>(null)
  const [longitude, setLongitude] = useState<number | null>(null)
  const [gpsNote, setGpsNote] = useState('')
  const [gpsBusy, setGpsBusy] = useState(false)
  const [budget, setBudget] = useState('')
  const [preferredStart, setPreferredStart] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [questions, setQuestions] = useState<Record<string, string>>({})
  const [descError, setDescError] = useState('')
  const [areaError, setAreaError] = useState('')
  const [dateError, setDateError] = useState('')
  const [cancellingId, setCancellingId] = useState('')

  function load() {
    setLoading(true)
    requestsApi
      .list({ page, pageSize: 10 })
      .then(async (result) => {
        setRows(result.items)
        setTotal(result.totalCount)
        const next: Record<string, QuoteDto[]> = {}
        await Promise.all(
          result.items.map(async (row) => {
            if (row.status === 'DRAFT') {
              next[row.id] = []
              return
            }
            try {
              next[row.id] = await marketplaceApi.requestQuotes(row.id)
            } catch {
              next[row.id] = []
            }
          }),
        )
        setQuotesByRequest(next)
        const nextQuestions: Record<string, string> = {}
        await Promise.all(
          result.items
            .filter((row) => row.status === 'CLARIFICATION_REQUIRED')
            .map(async (row) => {
              try {
                const history = await requestsApi.history(row.id)
                const note = [...history].reverse().find((item) => item.toStatus === 'CLARIFICATION_REQUIRED')?.note
                nextQuestions[row.id] = note?.trim() || 'Please add more detail so matching can continue.'
              } catch {
                nextQuestions[row.id] = 'Please add more detail so matching can continue.'
              }
            }),
        )
        setQuestions(nextQuestions)
        if (!selectedRequestId && result.items[0]) {
          setSelectedRequestId(result.items[0].id)
        }
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    void categoriesApi.list().then(setCategories).catch(() => undefined)
  }, [])

  useEffect(load, [page])

  async function create(event: FormEvent) {
    event.preventDefault()
    if (!description.trim()) {
      setDescError('Describe the job.')
      return
    }
    if (!serviceArea.trim()) {
      setAreaError('Enter the service area so matching can invite nearby technicians.')
      return
    }
    if (!preferredStart || !isFutureDateTime(preferredStart)) {
      setDateError('Choose a future date and time. Past dates are not allowed.')
      return
    }
    setDescError('')
    setAreaError('')
    setDateError('')
    try {
      const created = await requestsApi.create({
        categoryId: categoryId || undefined,
        description,
        serviceArea,
        address,
        latitude: latitude ?? undefined,
        longitude: longitude ?? undefined,
        budgetAmount: budget ? Number(budget) : undefined,
        preferredStart: preferredStart ? new Date(preferredStart).toISOString() : undefined,
        preferredEnd: preferredStart ? new Date(new Date(preferredStart).getTime() + 2 * 60 * 60 * 1000).toISOString() : undefined,
      })
      if (file) {
        await requestsApi.addMedia(created.id, file)
      }
      push('success', 'Request created as a draft.')
      setDescription('')
      setAddress('')
      setLatitude(null)
      setLongitude(null)
      setGpsNote('')
      setBudget('')
      setPreferredStart('')
      setFile(null)
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  async function cancelRequest(row: RequestDto) {
    if (!window.confirm('Cancel this request? Invited technicians and anyone who sent a quotation will be told you cancelled it.')) {
      return
    }
    setCancellingId(row.id)
    try {
      await requestsApi.cancel(row.id)
      push('success', 'Request cancelled. Technicians were notified.')
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    } finally {
      setCancellingId('')
    }
  }

  async function sendClarification(requestId: string) {
    const message = (answers[requestId] ?? '').trim()
    if (!message) {
      push('error', 'Type your answer in the box under this request, then send it.')
      setSelectedRequestId(requestId)
      return
    }
    try {
      await requestsApi.addClarification(requestId, message)
      push('success', 'Answer sent. Matching will continue.')
      setAnswers((current) => ({ ...current, [requestId]: '' }))
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  const columns: Column<RequestDto>[] = [
    { key: 'categoryName', header: 'Category', render: (row) => row.categoryName || '—' },
    { key: 'description', header: 'Description', render: (row) => row.description },
    { key: 'status', header: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'createdAt', header: 'Created', render: (row) => formatDate(row.createdAt) },
    {
      key: 'actions',
      header: '',
      render: (row) => (
        <div className="flex flex-wrap gap-2">
          {row.status === 'DRAFT' || row.status === 'ANALYZING' || row.status === 'FAILED' ? (
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  const updated = await requestsApi.submit(row.id)
                  push('success', updated.status === 'DRAFT' ? 'Request submitted.' : `Request is ${updated.status.replaceAll('_', ' ')}.`)
                  load()
                } catch (err) {
                  push('error', getApiError(err).error)
                  load()
                }
              }}
            >
              {row.status === 'DRAFT' ? 'Submit' : 'Continue'}
            </Button>
          ) : null}
          {row.status === 'CLARIFICATION_REQUIRED' ? (
            <Button
              variant="secondary"
              onClick={() => {
                setSelectedRequestId(row.id)
                document.getElementById(`request-answer-${row.id}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
              }}
            >
              Answer
            </Button>
          ) : null}
          <Button variant="ghost" onClick={() => setSelectedRequestId(row.id)}>
            {(quotesByRequest[row.id]?.length ?? 0) > 0
              ? `Quotes (${quotesByRequest[row.id].length})`
              : 'Quotes'}
          </Button>
          {['DRAFT', 'SUBMITTED', 'ANALYZING', 'CLARIFICATION_REQUIRED', 'MATCHING', 'COLLECTING_QUOTES', 'AWAITING_CUSTOMER_APPROVAL', 'BOOKED'].includes(row.status) ? (
            <Button variant="danger" disabled={cancellingId === row.id} onClick={() => void cancelRequest(row)}>
              {cancellingId === row.id ? 'Cancelling…' : 'Cancel request'}
            </Button>
          ) : null}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <PageHeader title="Requests" description="Create a draft, then submit it for matching." />
      {error ? <ErrorState message={error} /> : null}
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={create}>
        <FormField label="Category">
          <SelectInput value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
            <option value="">Select category</option>
            {categories.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </SelectInput>
        </FormField>
        <FormField label="Service area" hint="Example: Negombo or Jaffna. Matching invites verified technicians in that area." error={areaError}>
          <TextInput value={serviceArea} onChange={(event) => setServiceArea(event.target.value)} placeholder="Negombo" />
        </FormField>
        <FormField
          label="Address"
          hint="Technicians see the exact Google Maps pin only after you confirm the booking."
        >
          <div className="space-y-2">
            <TextInput
              value={address}
              onChange={(event) => setAddress(event.target.value)}
              placeholder="House number, street, town"
            />
            <Button
              type="button"
              variant="secondary"
              disabled={gpsBusy}
              onClick={() => {
                if (!navigator.geolocation) {
                  setGpsNote('This browser cannot share GPS. Type the address instead.')
                  return
                }
                setGpsBusy(true)
                navigator.geolocation.getCurrentPosition(
                  async (position) => {
                    const nextLat = position.coords.latitude
                    const nextLng = position.coords.longitude
                    setLatitude(nextLat)
                    setLongitude(nextLng)
                    setGpsNote(`GPS saved: ${nextLat.toFixed(5)}, ${nextLng.toFixed(5)}`)
                    try {
                      const reverse = await mapsApi.reverse(nextLat, nextLng)
                      if (reverse.displayName && !address.trim()) setAddress(reverse.displayName)
                      if (reverse.serviceArea && !serviceArea.trim()) setServiceArea(reverse.serviceArea)
                    } catch {
                      // Address text is optional when GPS is already captured.
                    } finally {
                      setGpsBusy(false)
                    }
                  },
                  () => {
                    setGpsNote('GPS was denied. Type the exact address instead.')
                    setGpsBusy(false)
                  },
                  { enableHighAccuracy: true, timeout: 12000 },
                )
              }}
            >
              {gpsBusy ? 'Reading GPS…' : 'Use my GPS location'}
            </Button>
            {gpsNote ? <p className="text-xs text-[#6d6a64]">{gpsNote}</p> : null}
          </div>
        </FormField>
        <FormField label="Preferred appointment" error={dateError} hint="Must be a future date and time.">
          <TextInput type="datetime-local" min={datetimeLocalMin()} value={preferredStart} onChange={(event) => setPreferredStart(event.target.value)} />
        </FormField>
        <FormField label="Budget (optional)">
          <TextInput type="number" min="1" value={budget} onChange={(event) => setBudget(event.target.value)} />
        </FormField>
        <FormField label="Problem photo">
          <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
        </FormField>
        <FormField label="Description" error={descError}>
          <TextArea value={description} onChange={(event) => setDescription(event.target.value)} />
        </FormField>
        <Button type="submit">Create draft</Button>
      </form>
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.id}
        loading={loading}
        emptyTitle="No requests"
        emptyDescription="Create a draft to get started."
        page={page}
        pageSize={10}
        totalCount={total}
        onPageChange={setPage}
      />
      <section className="space-y-3 rounded-2xl border border-black/8 bg-white p-5">
        <h2 className="font-semibold text-slate-900">Quotations received</h2>
        <p className="text-sm text-slate-500">
          We compare the real quotations technicians send. Pick the one you prefer, then confirm it on Bookings. FixFlow never books a technician for you. After you select a quote, the others stay visible for comparison.
        </p>
        {rows.length === 0 ? (
          <p className="text-sm text-slate-500">Create and submit a request first. Matching technicians can then send quotes.</p>
        ) : (
          rows.map((row) => {
            const quotes = quotesByRequest[row.id] ?? []
            const active = selectedRequestId === row.id
            const selectedQuote = quotes.find((item) => item.status === 'ACCEPTED')
            const selectionLocked =
              Boolean(selectedQuote) || ['BOOKED', 'COMPLETED', 'CANCELLED'].includes(row.status)
            return (
              <article
                key={row.id}
                className={`border p-4 ${active ? 'border-[#c4a574] bg-[#f4efe6]' : 'border-black/10 bg-white'}`}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="font-medium text-slate-900">{row.categoryName || 'Request'}</p>
                    <p className="text-sm text-slate-600">{row.description}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusBadge status={row.status} />
                    {['DRAFT', 'SUBMITTED', 'ANALYZING', 'CLARIFICATION_REQUIRED', 'MATCHING', 'COLLECTING_QUOTES', 'AWAITING_CUSTOMER_APPROVAL', 'BOOKED'].includes(row.status) ? (
                      <Button variant="danger" size="sm" disabled={cancellingId === row.id} onClick={() => void cancelRequest(row)}>
                        Cancel
                      </Button>
                    ) : null}
                  </div>
                </div>
                <p className="mt-2 text-sm text-[#6d6a64]">{requestHelpText(row.status)}</p>
                {row.status === 'CLARIFICATION_REQUIRED' ? (
                  <div id={`request-answer-${row.id}`} className="mt-3 space-y-3 border border-[#e6dccb] bg-[#faf7f1] p-4">
                    <p className="text-sm font-semibold text-slate-900">We need a little more information</p>
                    <p className="text-sm text-slate-700">{questions[row.id] || 'Please add more detail so matching can continue.'}</p>
                    <FormField label="Your answer">
                      <TextArea
                        value={answers[row.id] ?? ''}
                        onChange={(event) => setAnswers((current) => ({ ...current, [row.id]: event.target.value }))}
                        placeholder="Example: 2 plug switches need replacement. I can send a photo."
                      />
                    </FormField>
                    <Button type="button" onClick={() => void sendClarification(row.id)}>
                      Send answer
                    </Button>
                  </div>
                ) : null}
                {quotes.length === 0 ? (
                  <p className="mt-3 text-sm text-slate-500">No quotations yet for this request.</p>
                ) : (
                  <div className="mt-3 space-y-2">
                    {quotes.map((quote) => {
                      const isSelected = quote.status === 'ACCEPTED' || selectedQuote?.id === quote.id
                      return (
                      <div
                        key={quote.id}
                        className={`border bg-white p-4 text-sm ${isSelected ? 'border-[#c4a574] bg-[#f4efe6]' : 'border-black/10'}`}
                      >
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                          <div className="flex gap-3">
                            <TechnicianAvatar name={quote.technicianDisplayName} photoUrl={quote.profilePhotoUrl} size={48} />
                            <div>
                            <p className="font-semibold text-slate-900">
                              {quote.technicianDisplayName || 'Verified technician'} · {formatMoney(quote.totalAmount, quote.currency)}
                            </p>
                            <p className="mt-1 text-slate-600">
                              Labour {formatMoney(quote.labourAmount, quote.currency)} · materials {formatMoney(quote.materialsAmount, quote.currency)} ·
                              travel {formatMoney(quote.travelAmount, quote.currency)} · {quote.durationMinutes} min
                            </p>
                            <p className="mt-1 text-xs text-slate-500">
                              {quote.averageRating != null ? `Rating ${Number(quote.averageRating).toFixed(1)}/5` : 'No rating yet'}
                              {quote.reviewCount != null ? ` · ${quote.reviewCount} verified reviews` : ''}
                              {quote.completedJobs != null ? ` · ${quote.completedJobs} completed jobs` : ''}
                              {quote.arrivalStart ? ` · arrival ${formatDate(quote.arrivalStart)}` : ''}
                              {quote.distanceUnavailable || quote.approximateDistanceKm == null
                                ? ' · Distance unavailable'
                                : ` · ~${quote.approximateDistanceKm} km`}
                            </p>
                            {quote.recommendationSummary ? (
                              <p className="mt-2 text-slate-700">{quote.recommendationSummary}</p>
                            ) : null}
                            {(quote.strengths?.length ?? 0) > 0 ? (
                              <p className="mt-1 text-xs text-emerald-700">{quote.strengths?.join(' ')}</p>
                            ) : null}
                            {(quote.tradeoffs?.length ?? 0) > 0 ? (
                              <p className="mt-1 text-xs text-amber-700">{quote.tradeoffs?.join(' ')}</p>
                            ) : null}
                            </div>
                          </div>
                          {isSelected ? (
                            <StatusBadge status="ACCEPTED" />
                          ) : selectionLocked ? (
                            <p className="text-sm font-medium text-[#9a968e]">Not selected</p>
                          ) : quote.status === 'SENT' ? (
                            <Button
                              variant="secondary"
                              onClick={async () => {
                                try {
                                  await marketplaceApi.selectQuote(quote.id)
                                  push('success', 'Quote selected. Confirm the booking on the Bookings page. AI does not book automatically.')
                                  load()
                                  navigate('/customer/bookings')
                                } catch (err) {
                                  push('error', getApiError(err).error)
                                  load()
                                }
                              }}
                            >
                              Select
                            </Button>
                          ) : (
                            <StatusBadge status={quote.status} />
                          )}
                        </div>
                      </div>
                      )
                    })}
                  </div>
                )}
              </article>
            )
          })
        )}
      </section>
    </div>
  )
}
