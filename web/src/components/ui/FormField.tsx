import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { cn } from '../../utils/cn'

export function FormField({
  label,
  hint,
  error,
  children,
}: {
  label: string
  hint?: string
  error?: string
  children: ReactNode
}) {
  return (
    <div className="block text-sm">
      <span className="mb-1.5 block text-sm font-medium text-[#6d6a64]">{label}</span>
      {children}
      {hint && !error ? <span className="mt-1.5 block text-xs leading-5 text-slate-400">{hint}</span> : null}
      {error ? <span className="mt-1.5 block text-xs font-medium text-rose-600">{error}</span> : null}
    </div>
  )
}

const inputClass =
  'w-full rounded-xl border border-black/10 bg-white px-3.5 py-2.5 text-sm text-[#171717] placeholder:text-[#9a968e] shadow-sm transition duration-150 hover:border-[#c4a574] focus:border-[#c4a574] focus:outline-none focus:ring-4 focus:ring-[#c4a574]/15 disabled:cursor-not-allowed disabled:bg-[#f4efe6] disabled:text-[#9a968e]'

export function TextInput(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} className={cn(inputClass, props.className)} />
}

export function TextArea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea {...props} className={cn(inputClass, 'min-h-28 resize-y', props.className)} />
}

export function SelectInput(props: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select {...props} className={cn(inputClass, props.className)} />
}
