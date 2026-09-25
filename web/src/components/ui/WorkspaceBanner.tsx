export function WorkspaceBanner({
  kicker,
  title,
  description,
}: {
  kicker: string
  title: string
  description: string
}) {
  return (
    <section className="relative overflow-hidden rounded-[28px] bg-[#111318] px-6 py-9 text-white shadow-[var(--shadow-card)] sm:px-10 sm:py-12">
      <div className="absolute inset-y-0 left-0 w-1.5 bg-[#c4a574]" />
      <div className="absolute -top-20 -right-8 h-56 w-56 rounded-full bg-[#c4a574]/25 blur-3xl" />
      <div className="absolute -bottom-24 left-1/4 h-48 w-48 rounded-full bg-white/10 blur-3xl" />
      <div className="absolute right-8 bottom-8 hidden h-24 w-24 rounded-full border border-white/10 sm:block" />
      <div className="absolute right-16 top-10 hidden h-3 w-3 rounded-full bg-[#c4a574] sm:block" />
      <div className="relative max-w-2xl">
        <p className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-3 py-1 text-xs font-medium uppercase tracking-[0.18em] text-[#e6dccb]">
          <span className="h-1.5 w-1.5 rounded-full bg-[#c4a574]" />
          {kicker}
        </p>
        <h1 className="mt-5 text-3xl font-semibold tracking-tight sm:text-5xl">{title}</h1>
        <p className="mt-4 max-w-xl text-sm leading-7 text-white/70 sm:text-base">{description}</p>
      </div>
    </section>
  )
}
