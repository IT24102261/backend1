const steps = [
  {
    step: '01',
    title: 'Request a service',
    body: 'Describe the problem, add a photo and your preferred time.',
    image: 'https://images.unsplash.com/photo-1556912173-46c336c7fd55?auto=format&fit=crop&w=900&q=80',
  },
  {
    step: '02',
    title: 'Compare quotations',
    body: 'Approved technicians send real prices. We explain the differences so you can choose.',
    image: 'https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?auto=format&fit=crop&w=900&q=80',
  },
  {
    step: '03',
    title: 'Confirm the booking',
    body: 'You pick a quotation and confirm. We only check that it is still valid — we never book for you.',
    image: 'https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=900&q=80',
  },
]

export function HowItWorks() {
  return (
    <section id="how-it-works" className="scroll-mt-24 bg-[#faf7f1] py-24">
      <div className="mx-auto max-w-6xl px-4 text-center sm:px-6">
        <p className="landing-kicker text-[#c4a574]">How it works</p>
        <h2 className="landing-serif mt-4 text-4xl sm:text-5xl">Our latest workflow</h2>
        <div className="mt-14 grid gap-6 md:grid-cols-3">
          {steps.map((item) => (
            <article key={item.step} className="group relative overflow-hidden">
              <img src={item.image} alt="" className="h-80 w-full object-cover transition duration-500 group-hover:scale-105" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/20 to-transparent" />
              <div className="absolute inset-x-0 bottom-0 p-6 text-left text-white">
                <p className="text-sm font-medium text-[#c4a574]">{item.step}</p>
                <h3 className="landing-serif mt-2 text-2xl">{item.title}</h3>
                <p className="mt-2 text-sm leading-6 text-white/80">{item.body}</p>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}
