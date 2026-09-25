import { Clock3, MapPin, Star, UserRound } from 'lucide-react'

const columns = [
  {
    title: 'NEW REQUESTS',
    cards: [
      { id: 'REQ-1021', category: 'Electrical', title: 'Switch Replacement', area: 'Colombo', badge: 'New', price: 'LKR 4,500', rating: '4.8', initials: 'SK' },
      { id: 'REQ-1024', category: 'Plumbing', title: 'Pipe Leakage', area: 'Kandy', badge: 'New', price: 'LKR 6,200', rating: '4.6', initials: 'VM' },
    ],
  },
  {
    title: 'MATCHING',
    cards: [
      { id: 'REQ-1018', category: 'AC Repair', title: 'Cooling diagnosis', area: 'Jaffna', badge: 'Matching', price: 'LKR 8,000', rating: '4.9', initials: 'NR' },
    ],
  },
  {
    title: 'BOOKED',
    cards: [
      { id: 'BOOK-501', category: 'AC Repair', title: 'Technician Assigned', area: 'Negombo', badge: 'Booked', price: 'LKR 9,400', rating: '4.7', initials: 'AY' },
    ],
  },
  {
    title: 'COMPLETED',
    cards: [
      { id: 'JOB-220', category: 'Carpenter', title: 'Door hinge repair', area: 'Galle', badge: 'Done', price: 'LKR 3,800', rating: '5.0', initials: 'SR' },
    ],
  },
] as const

const badgeTone: Record<string, string> = {
  New: 'bg-sky-50 text-sky-700',
  Matching: 'bg-amber-50 text-amber-700',
  Booked: 'bg-blue-50 text-blue-700',
  Done: 'bg-emerald-50 text-emerald-700',
}

export function DashboardPreview() {
  return (
    <section className="px-4 pb-16 sm:px-6" aria-label="Landing page dashboard preview">
      <div className="landing-fade mx-auto max-w-[1000px] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-[0_24px_60px_rgb(15_23_42/0.12)]">
        <div className="flex items-center gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3">
          <div className="flex gap-1.5" aria-hidden="true">
            <span className="h-2.5 w-2.5 rounded-full bg-rose-400" />
            <span className="h-2.5 w-2.5 rounded-full bg-amber-400" />
            <span className="h-2.5 w-2.5 rounded-full bg-emerald-400" />
          </div>
          <p className="mx-auto rounded-lg bg-white px-3 py-1 text-xs text-slate-500 ring-1 ring-slate-200">
            app.fixflow.ai/dashboard
          </p>
        </div>
        <div className="p-5 sm:p-6">
          <p className="text-xs font-semibold tracking-[0.16em] text-slate-500 uppercase">Demo preview</p>
          <h2 className="mt-2 text-lg font-semibold text-slate-900">Service Request Board</h2>
          <p className="mt-1 text-sm text-slate-500">Track requests, quotations, bookings and technician progress</p>
          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {columns.map((column) => (
              <div key={column.title} className="rounded-xl border border-slate-200 bg-slate-50/70 p-3">
                <p className="mb-3 text-[11px] font-semibold tracking-wide text-slate-500">{column.title}</p>
                <div className="space-y-3">
                  {column.cards.map((card) => (
                    <article key={card.id} className="rounded-xl border border-slate-200 bg-white p-3 shadow-sm">
                      <div className="flex items-start justify-between gap-2">
                        <p className="text-xs font-semibold text-slate-500">{card.id}</p>
                        <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${badgeTone[card.badge]}`}>
                          {card.badge}
                        </span>
                      </div>
                      <p className="mt-2 text-sm font-semibold text-slate-900">{card.title}</p>
                      <p className="mt-1 text-xs text-slate-500">{card.category}</p>
                      <div className="mt-3 flex items-center justify-between text-[11px] text-slate-500">
                        <span className="inline-flex items-center gap-1">
                          <MapPin size={12} aria-hidden="true" />
                          {card.area}
                        </span>
                        <span className="inline-flex items-center gap-1">
                          <Clock3 size={12} aria-hidden="true" />
                          {card.price}
                        </span>
                      </div>
                      <div className="mt-3 flex items-center justify-between">
                        <span className="inline-flex items-center gap-1.5 text-xs text-slate-600">
                          <span className="flex h-6 w-6 items-center justify-center rounded-full bg-slate-900 text-[10px] font-semibold text-white">
                            {card.initials}
                          </span>
                          <UserRound size={12} aria-hidden="true" />
                        </span>
                        <span className="inline-flex items-center gap-1 text-xs font-medium text-slate-600">
                          <Star size={12} className="text-amber-500" aria-hidden="true" />
                          {card.rating}
                        </span>
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
