export function BarChart({ data }: { data: Array<{ label: string; value: number }> }) {
  const max = Math.max(...data.map((item) => item.value), 1)

  if (data.length === 0) {
    return <p className="rounded-2xl bg-[#faf7f1] px-4 py-6 text-sm text-[#6d6a64]">No chart data yet.</p>
  }

  return (
    <div className="space-y-4">
      {data.map((item) => (
        <div key={item.label}>
          <div className="mb-1.5 flex justify-between text-sm">
            <span className="font-medium text-[#6d6a64]">{item.label}</span>
            <span className="font-semibold text-[#171717]">{item.value}</span>
          </div>
          <div className="h-2.5 overflow-hidden rounded-full bg-[#f4efe6]">
            <div
              className="h-full rounded-full bg-gradient-to-r from-[#111318] to-[#c4a574]"
              style={{ width: `${Math.max((item.value / max) * 100, item.value ? 6 : 0)}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}

export function DonutChart({ data }: { data: Array<{ label: string; value: number; color: string }> }) {
  const total = data.reduce((sum, item) => sum + item.value, 0)
  let offset = 0

  return (
    <div className="flex flex-col items-center gap-5 sm:flex-row">
      <svg viewBox="0 0 42 42" className="h-40 w-40 -rotate-90" aria-hidden>
        <circle cx="21" cy="21" r="15.9" fill="transparent" stroke="#f4efe6" strokeWidth="6" />
        {total > 0
          ? data.map((item) => {
              const length = (item.value / total) * 100
              const circle = (
                <circle
                  key={item.label}
                  cx="21"
                  cy="21"
                  r="15.9"
                  fill="transparent"
                  stroke={item.color}
                  strokeWidth="6"
                  strokeDasharray={`${length} ${100 - length}`}
                  strokeDashoffset={-offset}
                />
              )
              offset += length
              return circle
            })
          : null}
      </svg>
      <ul className="space-y-2.5 text-sm">
        {data.map((item) => (
          <li key={item.label} className="flex items-center gap-2 text-[#6d6a64]">
            <span className="h-2.5 w-2.5 rounded-full" style={{ background: item.color }} />
            <span className="font-medium text-[#171717]">{item.label}</span>
            <span className="text-[#9a968e]">· {item.value}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
