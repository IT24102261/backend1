import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { cn } from '../../utils/cn'

export function DashboardSection({
  title,
  description,
  action,
  children,
  className,
}: {
  title: string
  description?: string
  action?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <section className={cn('rounded-3xl border border-black/8 bg-white p-6 shadow-[var(--shadow-card)]', className)}>
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold tracking-tight text-[#171717]">{title}</h2>
          {description ? <p className="mt-1 text-sm leading-6 text-[#6d6a64]">{description}</p> : null}
        </div>
        {action}
      </div>
      <div className="mt-5">{children}</div>
    </section>
  )
}

export function QuickAction({
  to,
  icon: Icon,
  title,
  description,
}: {
  to: string
  icon: LucideIcon
  title: string
  description: string
}) {
  return (
    <Link
      to={to}
      className="group flex items-start gap-4 rounded-2xl border border-black/8 bg-[#faf7f1] p-4 transition hover:-translate-y-0.5 hover:border-[#c4a574]/50 hover:bg-white hover:shadow-[var(--shadow-card)]"
    >
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-[#111318] text-[#e6dccb] transition group-hover:bg-[#c4a574] group-hover:text-[#111318]">
        <Icon size={18} />
      </span>
      <span>
        <span className="block font-semibold text-[#171717]">{title}</span>
        <span className="mt-1 block text-sm leading-6 text-[#6d6a64]">{description}</span>
      </span>
    </Link>
  )
}

export function ActivityRow({
  title,
  meta,
  badge,
}: {
  title: string
  meta: string
  badge?: ReactNode
}) {
  return (
    <li className="flex items-start justify-between gap-4 border-b border-black/5 py-3 last:border-0 last:pb-0">
      <div className="min-w-0">
        <p className="truncate font-medium text-[#171717]">{title}</p>
        <p className="mt-1 text-xs text-[#9a968e]">{meta}</p>
      </div>
      {badge}
    </li>
  )
}
