import type { ReactNode } from 'react'

export function ConfirmationModal({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  tone = 'default',
  busy,
  children,
  onConfirm,
  onClose,
}: {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  tone?: 'default' | 'danger'
  busy?: boolean
  children?: ReactNode
  onConfirm: () => void
  onClose: () => void
}) {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog" aria-modal>
      <div className="w-full max-w-md rounded-3xl border border-black/8 bg-white p-6 shadow-2xl">
        <h2 className="text-2xl font-semibold text-[#171717]">{title}</h2>
        <p className="mt-2 text-sm text-[#6d6a64]">{description}</p>
        {children ? <div className="mt-4">{children}</div> : null}
        <div className="mt-6 flex justify-end gap-2">
          <button
            type="button"
            className="rounded-xl border border-[#171717]/15 bg-white px-4 py-2 text-sm font-medium text-[#171717] hover:bg-[#f4efe6]"
            onClick={onClose}
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={busy}
            className={`rounded-xl px-4 py-2 text-sm font-medium text-white disabled:opacity-50 ${
              tone === 'danger' ? 'bg-rose-700 hover:bg-rose-600' : 'bg-[#171717] hover:bg-[#2a2a2a]'
            }`}
            onClick={onConfirm}
          >
            {busy ? 'Working…' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
