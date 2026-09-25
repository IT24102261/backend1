export function LoadingSpinner({ label = 'Loading' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-12 text-slate-500" role="status" aria-live="polite">
      <span className="h-5 w-5 animate-spin rounded-full border-2 border-[#e6dccb] border-t-[#171717]" />
      <span className="text-sm font-medium">{label}</span>
    </div>
  )
}
