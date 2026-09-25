import { CircleAlert } from 'lucide-react'
import type { ReactNode } from 'react'

export function ErrorState({
  title = 'Unable to load this page',
  message,
  action,
}: {
  title?: string
  message: string
  action?: ReactNode
}) {
  return (
    <div className="border border-rose-200 bg-rose-50 px-6 py-7" role="alert">
      <div className="flex items-start gap-3">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-rose-100 text-rose-700">
          <CircleAlert size={18} />
        </span>
        <div>
          <h3 className="text-base font-semibold text-rose-900">{title}</h3>
          <p className="mt-1 text-sm leading-6 text-rose-800">{message}</p>
          {action ? <div className="mt-4">{action}</div> : null}
        </div>
      </div>
    </div>
  )
}
