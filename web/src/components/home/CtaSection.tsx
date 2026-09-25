import { Link } from 'react-router-dom'

export function CtaSection() {
  return (
    <section className="relative overflow-hidden bg-[#111318] py-24 text-center text-white">
      <img
        src="https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=1800&q=80"
        alt=""
        className="absolute inset-0 h-full w-full object-cover opacity-25"
      />
      <div className="relative mx-auto max-w-3xl px-4 sm:px-6">
        <p className="landing-kicker text-[#c4a574]">Start a request</p>
        <h2 className="landing-serif mt-4 text-4xl sm:text-5xl">Ready when your home needs help</h2>
        <p className="mx-auto mt-5 max-w-xl text-sm leading-7 text-white/70">
          Create a FixFlow account and connect with eligible technicians through a safer, more transparent workflow.
        </p>
        <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <Link to="/register?role=customer" className="landing-btn landing-btn-ghost bg-white text-[#111318]">
            Create customer account
          </Link>
          <Link to="/register?role=technician" className="landing-btn landing-btn-ghost">
            Become a technician
          </Link>
        </div>
      </div>
    </section>
  )
}
