export const customerBookingSteps = [
  {
    status: 'PENDING_VALIDATION',
    title: 'Confirm your booking',
    detail: 'You picked this quote. Confirm it to book the technician.',
  },
  {
    status: 'CONFIRMED',
    title: 'Booking confirmed',
    detail: 'Your booking is confirmed. The technician can now see your address.',
  },
  {
    status: 'ACCEPTED',
    title: 'Technician accepted',
    detail: 'The technician accepted the job and is getting ready.',
  },
  {
    status: 'EN_ROUTE',
    title: 'On the way',
    detail: 'The technician is travelling to your location.',
  },
  {
    status: 'IN_PROGRESS',
    title: 'Work in progress',
    detail: 'The technician is working at your place.',
  },
  {
    status: 'WORK_COMPLETED',
    title: 'Work finished',
    detail: 'The technician marked the job as finished. Confirm if you are happy with the work.',
  },
  {
    status: 'CUSTOMER_CONFIRMED',
    title: 'You confirmed the work',
    detail: 'You confirmed the work was completed.',
  },
] as const

const bookingLabels: Record<string, string> = {
  PENDING_VALIDATION: 'Waiting for your confirmation',
  CONFIRMED: 'Booking confirmed',
  ACCEPTED: 'Technician accepted',
  EN_ROUTE: 'On the way',
  IN_PROGRESS: 'Work in progress',
  WORK_COMPLETED: 'Work finished',
  CUSTOMER_CONFIRMED: 'You confirmed the work',
  CLOSED: 'Job closed',
  DISPUTED: 'Dispute opened',
  CANCELLED: 'Booking cancelled',
}

const noteLabels: Record<string, string> = {
  'quote selected after agent 4 validation': 'You selected this quotation.',
  'customer confirmed booking after agent 4 validation': 'You confirmed the booking.',
  'customer opened a dispute': 'You opened a dispute.',
}

export function formatBookingStatus(status?: string | null) {
  if (!status) return 'Unknown'
  return bookingLabels[status] ?? status.replaceAll('_', ' ')
}

export function bookingStatusDetail(status?: string | null) {
  if (status === 'CLOSED') return 'This job is closed.'
  if (status === 'DISPUTED') return 'A dispute is open on this booking. Support will review it.'
  if (status === 'CANCELLED') return 'This booking was cancelled and will not continue.'
  return customerBookingSteps.find((step) => step.status === status)?.detail ?? 'Follow the steps below to see where this job is.'
}

export function bookingStepIndex(status?: string | null) {
  if (status === 'CLOSED') return customerBookingSteps.length - 1
  return customerBookingSteps.findIndex((step) => step.status === status)
}

export function friendlyBookingNote(note?: string | null, fromStatus?: string | null, toStatus?: string | null) {
  const normalised = note?.trim().toLowerCase() ?? ''
  if (normalised && noteLabels[normalised]) return noteLabels[normalised]
  if (fromStatus === toStatus && toStatus === 'PENDING_VALIDATION') return 'You selected this quotation.'
  if (toStatus === 'CONFIRMED') return 'You confirmed the booking.'
  if (toStatus === 'ACCEPTED') return 'The technician accepted the job.'
  if (toStatus === 'EN_ROUTE') return 'The technician is on the way.'
  if (toStatus === 'IN_PROGRESS') return 'The technician started the work.'
  if (toStatus === 'WORK_COMPLETED') return 'The technician marked the work as finished.'
  if (toStatus === 'CUSTOMER_CONFIRMED') return 'You confirmed the work was completed.'
  if (toStatus === 'DISPUTED') return 'A dispute was opened on this booking.'
  if (toStatus === 'CANCELLED') return 'This booking was cancelled.'
  if (toStatus === 'CLOSED') return 'This job was closed.'
  return note?.trim() || formatBookingStatus(toStatus)
}
