import { Link } from 'react-router-dom'

export function Hero() {
  return (
    <section id="home" className="relative min-h-[78vh] scroll-mt-24 overflow-hidden">
      <img
        src="https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=2000&q=80"
        alt="Bright living room ready for a home service visit"
        className="absolute inset-0 h-full w-full object-cover"
      />
      <div className="absolute inset-0 bg-gradient-to-r from-[#f4efe6]/92 via-[#f4efe6]/70 to-transparent" />
      <div className="relative mx-auto flex min-h-[78vh] max-w-6xl items-center px-4 py-20 sm:px-6">
        <div className="max-w-xl rounded-3xl bg-[#f4efe6]/85 p-7 shadow-[var(--shadow-card)] backdrop-blur-sm sm:p-8">
          <p className="landing-fade landing-kicker text-[#6d6a64]">Building trust, one home at a time</p>
          <h1 className="landing-fade landing-delay-1 landing-serif mt-5 text-5xl leading-tight font-semibold tracking-tight text-[#171717] sm:text-6xl lg:text-7xl">
            Trusted home
            <br />
            services.
            <br />
            Lasting value.
          </h1>
          <p className="landing-fade landing-delay-2 mt-6 max-w-md text-sm leading-7 text-[#4f4c47] sm:text-base">
            FixFlow AI connects customers with verified technicians, compares real quotations, and keeps the final
            booking decision with you.
          </p>
          <div className="landing-fade landing-delay-3 mt-8 flex flex-col gap-3 sm:flex-row">
            <Link to="/register?role=customer" className="landing-btn landing-btn-dark">
              Find a technician
            </Link>
            <a href="#services" className="landing-btn landing-btn-light">
              Our services
            </a>
          </div>
        </div>
      </div>
    </section>
  )
}
