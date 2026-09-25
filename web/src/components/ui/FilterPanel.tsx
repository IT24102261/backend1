import type { ReactNode } from 'react'

export function FilterPanel({ children }: { children: ReactNode }) {
  return (
    <div className="flex flex-col gap-3 rounded-2xl border border-black/8 bg-white p-4 shadow-sm sm:flex-row sm:items-center">
      {children}
    </div>
  )
}

export function SelectFilter({
  label,
  value,
  onChange,
  options,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  options: Array<{ value: string; label: string }>
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block text-sm font-medium text-[#6d6a64]">{label}</span>
      <select
        className="w-full min-w-40 rounded-xl border border-black/10 bg-white px-3 py-2 text-sm text-[#171717] hover:border-[#c4a574] focus:border-[#c4a574] focus:outline-none"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  )
}
