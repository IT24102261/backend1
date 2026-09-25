import type { LucideIcon } from 'lucide-react'
import { cn } from '../../utils/cn'

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  accent = 'gold',
}: {
  label: string
  value: string | number
  hint?: string
  icon: LucideIcon
  accent?: 'gold' | 'navy' | 'cream'
}) {
  const tones = {
    gold: 'bg-[#f4efe6] text-[#c4a574]',
    navy: 'bg-[#111318] text-[#e6dccb]',
    cream: 'bg-[#faf7f1] text-[#7a6240] ring-1 ring-[#e6dccb]',
  }

  return (
    <article className="group relative overflow-hidden rounded-3xl border border-black/8 bg-white p-5 shadow-[var(--shadow-card)] transition duration-200 hover:-translate-y-1 hover:border-[#c4a574]/40 hover:shadow-[var(--shadow-card-hover)]">
      <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-[#c4a574] via-[#e6dccb] to-transparent" />
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-[#9a968e]">{label}</p>
          <p className="mt-3 text-3xl font-semibold tracking-tight text-[#171717]">{value}</p>
          {hint ? <p className="mt-2 text-xs leading-5 text-[#6d6a64]">{hint}</p> : null}
        </div>
        <span className={cn('flex h-12 w-12 items-center justify-center rounded-2xl transition group-hover:scale-105', tones[accent])}>
          <Icon size={18} />
        </span>
      </div>
    </article>
  )
}
