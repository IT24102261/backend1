import { Inbox } from 'lucide-react'
import type { ReactNode } from 'react'

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <div className="rounded-2xl border border-dashed border-[#e6dccb] bg-[#faf7f1] px-6 py-14 text-center">
      <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-[#c4a574] shadow-sm">
        <Inbox size={20} />
      </span>
      <h3 className="mt-4 text-xl font-semibold text-[#171717]">{title}</h3>
      <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[#6d6a64]">{description}</p>
      {action ? <div className="mt-5">{action}</div> : null}
    </div>
  )
}
