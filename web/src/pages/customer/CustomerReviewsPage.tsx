import { useEffect, useState, type FormEvent } from 'react'
import { marketplaceApi } from '../../api/marketplace'
import { reviewsApi } from '../../api/reviews'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, SelectInput, TextArea, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToastStore } from '../../store/toastStore'
import type { BookingDto, ReviewDto } from '../../types/api'
import { formatDate, shortId } from '../../utils/format'
import { getApiError } from '../../utils/errors'

export function CustomerReviewsPage() {
  const push = useToastStore((state) => state.push)
  const [bookings, setBookings] = useState<BookingDto[]>([])
  const [reviews, setReviews] = useState<ReviewDto[]>([])
  const [bookingId, setBookingId] = useState('')
  const [rating, setRating] = useState('5')
  const [body, setBody] = useState('')
  const [error, setError] = useState('')
  const [validation, setValidation] = useState('')

  function load() {
    Promise.all([
      marketplaceApi.bookings({ page: 1, pageSize: 50, status: 'CUSTOMER_CONFIRMED' }),
      reviewsApi.mine(),
    ])
      .then(([result, items]) => {
        const reviewed = new Set(items.map((item) => item.bookingId))
        setBookings(result.items.filter((item) => !reviewed.has(item.id)))
        setReviews(items)
      })
      .catch((err) => setError(getApiError(err).error))
  }

  useEffect(load, [])

  async function submit(event: FormEvent) {
    event.preventDefault()
    const value = Number(rating)
    if (!bookingId) {
      setValidation('Select a completed booking.')
      return
    }
    if (value < 1 || value > 5) {
      setValidation('Rating must be between 1 and 5.')
      return
    }
    setValidation('')
    try {
      await reviewsApi.create(bookingId, value, body)
      push('success', 'Review submitted.')
      setBody('')
      load()
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Reviews" description="You can review a technician only after confirming the completed work." />
      {error ? <ErrorState message={error} /> : null}
      {bookings.length === 0 && !error ? (
        <EmptyState title="No reviewable bookings" description="Confirm a completed job before leaving a review." />
      ) : (
        <form className="space-y-4 rounded-2xl border border-black/8 bg-white p-5" onSubmit={submit}>
          <FormField label="Booking">
            <SelectInput value={bookingId} onChange={(event) => setBookingId(event.target.value)}>
              <option value="">Select booking and technician</option>
              {bookings.map((item) => (
                <option key={item.id} value={item.id}>
                  {shortId(item.id)} · {item.technicianDisplayName || 'Technician'}
                </option>
              ))}
            </SelectInput>
          </FormField>
          <FormField label="Rating" error={validation.includes('Rating') ? validation : undefined}>
            <TextInput type="number" min="1" max="5" value={rating} onChange={(event) => setRating(event.target.value)} />
          </FormField>
          <FormField label="Comments">
            <TextArea value={body} onChange={(event) => setBody(event.target.value)} />
          </FormField>
          {validation && !validation.includes('Rating') ? <p className="text-sm text-rose-600">{validation}</p> : null}
          <Button type="submit">Submit review</Button>
        </form>
      )}
      <div className="space-y-3">
        {reviews.map((review) => (
          <article key={review.id} className="rounded-2xl border border-black/8 bg-white p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="font-semibold text-[#171717]">{review.rating} / 5 · {review.technicianDisplayName || 'Technician'}</p>
              <StatusBadge status={review.status} />
            </div>
            <p className="mt-1 text-xs text-[#9a968e]">Booking {shortId(review.bookingId)}</p>
            <p className="mt-2 text-sm text-[#6d6a64]">{review.body || 'No comment'}</p>
            {review.adminReply ? (
              <p className="mt-3 rounded-xl bg-[#f4efe6] px-3 py-2 text-sm text-[#171717]">
                <span className="font-medium">Admin reply:</span> {review.adminReply}
              </p>
            ) : null}
            <p className="mt-2 text-xs text-[#9a968e]">{formatDate(review.createdAt)}</p>
          </article>
        ))}
      </div>
    </div>
  )
}
