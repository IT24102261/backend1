import { Check } from 'lucide-react'
import { cn } from '../../utils/cn'
import {
  bookingStatusDetail,
  bookingStepIndex,
  customerBookingSteps,
  formatBookingStatus,
  friendlyBookingNote,
} from '../../utils/bookingStatus'
import { formatDate } from '../../utils/format'
import type { BookingDto, BookingHistoryDto } from '../../types/api'
import { TechnicianAvatar } from './TechnicianAvatar'

function stepTime(history: BookingHistoryDto[], status: string) {
  const match = [...history].reverse().find((item) => item.toStatus === status)
  return match?.timestamp
}

export function BookingTracker({
  booking,
  history,
}: {
  booking: BookingDto
  history: BookingHistoryDto[]
}) {
  const currentIndex = bookingStepIndex(booking.status)
  const interrupted = booking.status === 'DISPUTED' || booking.status === 'CANCELLED'
  const reachedIndex = Math.max(
    currentIndex,
    ...history.map((item) => bookingStepIndex(item.toStatus)).filter((index) => index >= 0),
    -1,
  )

  return (
    <section className="space-y-6 rounded-2xl border border-black/8 bg-white p-5">
      <div className="flex items-start gap-4">
        <TechnicianAvatar name={booking.technicianDisplayName} photoUrl={booking.profilePhotoUrl} size={64} />
        <div>
        <p className="text-xs font-medium uppercase tracking-wide text-[#9a968e]">Track your job</p>
        <h2 className="mt-1 text-2xl font-semibold text-[#171717]">{formatBookingStatus(booking.status)}</h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-[#6d6a64]">
          {booking.technicianDisplayName ? `${booking.technicianDisplayName} · ` : ''}
          {bookingStatusDetail(booking.status)}
        </p>
        </div>
      </div>

      {interrupted ? (
        <p className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
          {bookingStatusDetail(booking.status)}
        </p>
      ) : null}

      <ol className="space-y-0">
        {customerBookingSteps.map((step, index) => {
          const done = !interrupted && currentIndex >= 0 && index < currentIndex
          const current = !interrupted && index === currentIndex
          const reached = interrupted && index <= reachedIndex && reachedIndex >= 0
          const latest = current || (interrupted && index === reachedIndex)
          const time = stepTime(history, step.status)
          return (
            <li key={step.status} className="flex gap-4">
              <div className="flex flex-col items-center">
                <span
                  className={cn(
                    'flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-semibold',
                    latest
                      ? 'bg-[#111318] text-white'
                      : done || reached
                        ? 'bg-emerald-600 text-white'
                        : 'bg-white text-[#9a968e] ring-1 ring-black/10',
                  )}
                >
                  {done || reached ? <Check className="h-4 w-4" /> : index + 1}
                </span>
                {index < customerBookingSteps.length - 1 ? (
                  <span className={cn('min-h-10 w-px flex-1', done || reached || current ? 'bg-[#c4a574]' : 'bg-black/10')} />
                ) : null}
              </div>
              <div className={cn('pb-6', latest ? 'rounded-2xl bg-[#faf7f1] px-4 py-3' : 'pt-1')}>
                <p className="font-semibold text-[#171717]">{step.title}</p>
                <p className="mt-1 text-sm leading-6 text-[#6d6a64]">{step.detail}</p>
                {time ? <p className="mt-1 text-xs text-[#9a968e]">{formatDate(time)}</p> : null}
                {latest ? <p className="mt-2 text-xs font-medium uppercase tracking-wide text-[#c4a574]">Current step</p> : null}
              </div>
            </li>
          )
        })}
      </ol>

      {history.length > 0 ? (
        <div className="border-t border-black/5 pt-5">
          <h3 className="font-semibold text-[#171717]">What happened</h3>
          <ol className="mt-3 space-y-3">
            {history.map((item, index) => (
              <li key={item.id} className="flex gap-3 text-sm">
                <span className="mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full bg-[#c4a574]" />
                <div>
                  <p className="font-medium text-[#171717]">{friendlyBookingNote(item.note, item.fromStatus, item.toStatus)}</p>
                  <p className="mt-0.5 text-xs text-[#9a968e]">
                    {formatDate(item.timestamp)}
                    {index === history.length - 1 ? ' · Latest update' : ''}
                  </p>
                </div>
              </li>
            ))}
          </ol>
        </div>
      ) : (
        <p className="text-sm text-[#6d6a64]">Updates will appear here as the technician moves through the job.</p>
      )}
    </section>
  )
}
