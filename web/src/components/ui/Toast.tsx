import { CircleAlert, CircleCheck, Info, X } from 'lucide-react'
import { useToastStore } from '../../store/toastStore'

const icons = {
  success: CircleCheck,
  error: CircleAlert,
  info: Info,
}

export function Toast() {
  const toasts = useToastStore((state) => state.toasts)
  const dismiss = useToastStore((state) => state.dismiss)

  return (
    <div className="pointer-events-none fixed right-4 bottom-4 z-50 flex w-full max-w-sm flex-col gap-2">
      {toasts.map((toast) => {
        const Icon = icons[toast.kind]
        return (
          <div
            key={toast.id}
            className={`pointer-events-auto flex items-start gap-3 rounded-2xl border bg-white/95 p-4 shadow-xl backdrop-blur ${
              toast.kind === 'error'
                ? 'border-rose-200'
                : toast.kind === 'success'
                  ? 'border-emerald-200'
                  : 'border-slate-200'
            }`}
            role="status"
          >
            <Icon size={18} className={toast.kind === 'error' ? 'text-rose-600' : toast.kind === 'success' ? 'text-emerald-700' : 'text-[#c4a574]'} />
            <p className="flex-1 text-sm leading-5 text-slate-700">{toast.message}</p>
            <button type="button" className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700" onClick={() => dismiss(toast.id)} aria-label="Dismiss notification">
              <X size={14} />
            </button>
          </div>
        )
      })}
    </div>
  )
}
