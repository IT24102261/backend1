import type { WorkflowStepDto } from '../types/api'

export type AgentStage = {
  key: string
  title: string
  purpose: string
}

export const agentStages: AgentStage[] = [
  {
    key: 'Planning Agent',
    title: 'Understanding your request',
    purpose: 'We read what you wrote and identified the type of technician you need.',
  },
  {
    key: 'Matching Agent',
    title: 'Finding technicians',
    purpose: 'We invited only technicians who are approved for this kind of work.',
  },
  {
    key: 'Recommendation Agent',
    title: 'Comparing quotations',
    purpose: 'We explained the quotes you received. You still choose — we never book for you.',
  },
  {
    key: 'Validation Agent',
    title: 'Checking your booking',
    purpose: 'We checked that the quote you picked is still valid before the job can start.',
  },
]

const roleToStage: Record<string, string> = {
  REQUEST_PLANNING: 'Planning Agent',
  INTAKE: 'Planning Agent',
  ORCHESTRATOR: 'Planning Agent',
  TECHNICIAN_MATCHING: 'Matching Agent',
  MATCHING: 'Matching Agent',
  QUOTATION_RECOMMENDATION: 'Recommendation Agent',
  QUOTATION: 'Recommendation Agent',
  BOOKING_VALIDATION: 'Validation Agent',
  SAFETY: 'Validation Agent',
  SUPPORT: 'Validation Agent',
}

export function stageKey(role: string) {
  return roleToStage[role] ?? role.replaceAll('_', ' ')
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : null
}

function count(value: unknown) {
  return Array.isArray(value) ? value.length : 0
}

function namesFromPeople(value: unknown) {
  if (!Array.isArray(value)) return []
  return value
    .map((item) => {
      const row = asRecord(item)
      return (row?.displayName ?? row?.name ?? row?.technicianDisplayName) as string | undefined
    })
    .filter((name): name is string => Boolean(name && name.trim()))
}

function stringList(value: unknown) {
  if (!Array.isArray(value)) return []
  return value.filter((item): item is string => typeof item === 'string' && item.trim().length > 0)
}

function percent(value: unknown) {
  if (typeof value !== 'number' || Number.isNaN(value)) return null
  const ratio = value > 1 ? value : value * 100
  return Math.round(ratio)
}

export function explainStep(step: WorkflowStepDto): { headline: string; points: string[] } {
  let parsed: Record<string, unknown> = {}
  try {
    parsed = JSON.parse(step.outputJson || '{}') as Record<string, unknown>
  } catch {
    parsed = {}
  }

  const stage = stageKey(step.agentRole)
  if (stage === 'Planning Agent') return explainPlanning(parsed)
  if (stage === 'Matching Agent') return explainMatching(parsed)
  if (stage === 'Recommendation Agent') return explainRecommendation(parsed)
  if (stage === 'Validation Agent') return explainValidation(parsed)
  return { headline: 'Update recorded', points: [] }
}

function explainPlanning(data: Record<string, unknown>) {
  const category = String(data.category ?? data.requiredTechnician ?? '').trim()
  const subcategory = String(data.subcategory ?? '').trim()
  const confidence = percent(data.confidence)
  const missing = count(data.missingInformation)
  const needsMore = data.clarificationRequired === true
  const plan = stringList(data.plan)
  const questions = stringList(data.clarificationQuestions)

  const headline = category
    ? `This looks like a ${category.toLowerCase()} job${subcategory ? ` (${subcategory})` : ''}.`
    : 'We classified your request.'

  const points: string[] = []
  if (confidence != null) {
    points.push(
      confidence >= 80
        ? `We’re confident this is the right trade (${confidence}%).`
        : `We’re less sure about the trade (${confidence}%). You may be asked a follow-up question.`,
    )
  }
  if (needsMore) {
    points.push('We need a bit more information from you before matching technicians.')
  } else {
    points.push('We had enough detail to continue. You do not need to answer extra questions right now.')
  }
  if (missing > 0) {
    points.push(`${missing} detail${missing === 1 ? '' : 's'} were flagged as missing.`)
  }
  points.push(...plan.map((item) => `Next: ${item}`))
  points.push(...questions.map((item) => `Question: ${item}`))
  return { headline, points }
}

function explainMatching(data: Record<string, unknown>) {
  const eligible = count(data.eligibleTechnicians ?? data.eligible)
  const excluded = count(data.excludedTechnicians)
  const names = namesFromPeople(data.eligibleTechnicians)
  const headline =
    eligible === 0
      ? 'No approved technicians were available for this job yet.'
      : `We found ${eligible} approved technician${eligible === 1 ? '' : 's'} for this job.`

  const points: string[] = []
  if (names.length > 0) {
    points.push(`Invited: ${names.slice(0, 6).join(', ')}${names.length > 6 ? ` and ${names.length - 6} more` : ''}.`)
  }
  if (excluded > 0) {
    points.push(`${excluded} other technician${excluded === 1 ? ' was' : 's were'} skipped because they are not approved for this trade or were not available.`)
  } else if (eligible > 0) {
    points.push('Only category-approved technicians were invited.')
  }
  return { headline, points }
}

function explainRecommendation(data: Record<string, unknown>) {
  const options = Array.isArray(data.options) ? data.options : []
  const headline =
    options.length === 0
      ? 'No quotations were ready to compare yet.'
      : `We compared ${options.length} quotation${options.length === 1 ? '' : 's'} for you.`

  const points = options.flatMap((item, index) => {
    const row = asRecord(item)
    const summary = String(row?.summary ?? '').trim()
    const strengths = stringList(row?.strengths)
    const tradeoffs = stringList(row?.tradeoffs)
    const lines = [`Quote ${index + 1}${summary ? `: ${summary}` : ''}`]
    if (strengths.length) lines.push(`Good fit: ${strengths.join(' ')}`)
    if (tradeoffs.length) lines.push(`Keep in mind: ${tradeoffs.join(' ')}`)
    return lines
  })
  points.push('This is advice only. You pick the quote, then confirm the booking yourself.')
  return { headline, points }
}

function explainValidation(data: Record<string, unknown>) {
  const valid = data.valid === true
  const allowed = data.bookingAllowed === true
  const errors = stringList(data.errors)
  const headline = allowed
    ? 'Your selected quotation checked out. You can confirm the booking.'
    : valid
      ? 'The quotation looks valid, but booking is not allowed yet.'
      : 'We could not approve this booking yet.'

  const points: string[] = []
  if (allowed) {
    points.push('The quote still belongs to your request, has not expired, and the technician is available.')
  }
  points.push(...errors.map((item) => `Issue: ${item}`))
  if (!allowed && errors.length === 0) {
    points.push('Confirm the booking on the Bookings page if you have selected a quote.')
  }
  return { headline, points }
}

export function stageStatusLabel(done: boolean) {
  return done ? 'Done' : 'Waiting'
}
