import { Search } from 'lucide-react'

export function SearchBar({
  value,
  onChange,
  placeholder = 'Search',
}: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
}) {
  return (
    <label className="relative block min-w-[220px] flex-1">
      <span className="sr-only">Search</span>
      <Search size={16} className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-[#9a968e]" />
      <input
        className="w-full rounded-xl border border-black/10 bg-white py-2.5 pr-3 pl-9 text-sm text-[#171717] shadow-sm placeholder:text-[#9a968e] hover:border-[#c4a574] focus:border-[#c4a574] focus:outline-none focus:ring-4 focus:ring-[#c4a574]/15"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  )
}
