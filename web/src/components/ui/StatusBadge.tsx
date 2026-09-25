import { cn } from '../../utils/cn'
import { formatStatus } from '../../utils/format'

const tones: Record<string, string> = {
  healthy: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  DRAFT: 'bg-slate-100 text-slate-700 ring-slate-200',
  SUBMITTED: 'bg-[#111318] text-[#f4efe6] ring-[#111318]',
  ANALYZING: 'bg-indigo-50 text-indigo-700 ring-indigo-200',
  CLARIFICATION_REQUIRED: 'bg-amber-50 text-amber-800 ring-amber-200',
  MATCHING: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  COLLECTING_QUOTES: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  AWAITING_CUSTOMER_APPROVAL: 'bg-orange-50 text-orange-800 ring-orange-200',
  BOOKED: 'bg-blue-50 text-blue-700 ring-blue-200',
  COMPLETED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  CANCELLED: 'bg-slate-100 text-slate-500 ring-slate-200',
  FAILED: 'bg-rose-50 text-rose-700 ring-rose-200',
  APPROVED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  REJECTED: 'bg-rose-50 text-rose-700 ring-rose-200',
  PENDING: 'bg-amber-50 text-amber-800 ring-amber-200',
  PUBLISHED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  HIDDEN: 'bg-slate-100 text-slate-600 ring-slate-200',
  REMOVED: 'bg-rose-50 text-rose-700 ring-rose-200',
  SENT: 'bg-[#111318] text-[#f4efe6] ring-[#111318]',
  ACCEPTED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  DECLINED: 'bg-rose-50 text-rose-700 ring-rose-200',
  WITHDRAWN: 'bg-slate-100 text-slate-600 ring-slate-200',
  PENDING_VALIDATION: 'bg-amber-50 text-amber-800 ring-amber-200',
  CONFIRMED: 'bg-blue-50 text-blue-700 ring-blue-200',
  EN_ROUTE: 'bg-indigo-50 text-indigo-700 ring-indigo-200',
  IN_PROGRESS: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  WORK_COMPLETED: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  CUSTOMER_CONFIRMED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  CLOSED: 'bg-slate-100 text-slate-700 ring-slate-200',
  DISPUTED: 'bg-rose-50 text-rose-700 ring-rose-200',
  OPEN: 'bg-amber-50 text-amber-800 ring-amber-200',
  IN_REVIEW: 'bg-indigo-50 text-indigo-700 ring-indigo-200',
  RESOLVED: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  DISMISSED: 'bg-slate-100 text-slate-600 ring-slate-200',
  RUNNING: 'bg-[#111318] text-[#f4efe6] ring-[#111318]',
  CREATED: 'bg-slate-100 text-slate-700 ring-slate-200',
  PLANNING: 'bg-indigo-50 text-indigo-700 ring-indigo-200',
  QUOTE_COLLECTION: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  RECOMMENDING: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  WAITING_FOR_CUSTOMER_APPROVAL: 'bg-orange-50 text-orange-800 ring-orange-200',
  WAITING_APPROVAL: 'bg-amber-50 text-amber-800 ring-amber-200',
  VALIDATING: 'bg-indigo-50 text-indigo-700 ring-indigo-200',
  MORE_INFORMATION_REQUIRED: 'bg-orange-50 text-orange-800 ring-orange-200',
  SUCCESS: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  ACTIVE: 'bg-[#f4efe6] text-[#7a6240] ring-[#e6dccb]',
  SUSPENDED: 'bg-rose-50 text-rose-700 ring-rose-200',
}

export function StatusBadge({ status, label }: { status: string; label?: string }) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ring-1 ring-inset',
        tones[status] ?? 'bg-slate-100 text-slate-600 ring-slate-200',
      )}
    >
      {label ?? formatStatus(status)}
    </span>
  )
}
