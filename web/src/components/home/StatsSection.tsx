import { Clock3, ShieldCheck, Users } from 'lucide-react'

const stats = [
  { label: 'Verified technicians', detail: 'Category approval before any invitation is sent.', icon: ShieldCheck },
  { label: 'Real quotations', detail: 'Compare price, arrival, distance and reputation.', icon: Users },
  { label: 'Customer approval', detail: 'AI never books a technician without your confirm step.', icon: Clock3 },
]

export function StatsSection() {
  return (
    <section className="border-y border-black/5 bg-white py-12">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 sm:grid-cols-3 sm:px-6">
        {stats.map((item) => {
          const Icon = item.icon
          return (
            <article key={item.label} className="flex items-start gap-4">
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full border border-[#171717]/15 text-[#c4a574]">
                <Icon size={18} aria-hidden="true" />
              </span>
              <div>
                <p className="text-sm font-semibold tracking-wide text-[#171717] uppercase">{item.label}</p>
                <p className="mt-1 text-sm leading-6 text-[#6d6a64]">{item.detail}</p>
              </div>
            </article>
          )
        })}
      </div>
    </section>
  )
}
