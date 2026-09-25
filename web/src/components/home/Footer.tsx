import { Link } from 'react-router-dom'
import { House } from 'lucide-react'

export function LandingFooter() {
  return (
    <footer className="bg-[#faf7f1]">
      <div className="mx-auto grid max-w-6xl gap-10 px-4 py-16 sm:px-6 md:grid-cols-4">
        <div>
          <p className="inline-flex items-center gap-2 text-sm font-semibold">
            <House size={16} aria-hidden="true" />
            FixFlow AI
          </p>
          <p className="mt-4 text-sm leading-7 text-[#6d6a64]">
            AI-powered home service matching with verified technicians and customer approval.
          </p>
        </div>
        <div>
          <p className="landing-kicker text-[#171717]">Platform</p>
          <ul className="mt-4 space-y-2 text-sm text-[#6d6a64]">
            <li><a href="#services">Services</a></li>
            <li><a href="#how-it-works">How it works</a></li>
          </ul>
        </div>
        <div>
          <p className="landing-kicker text-[#171717]">Users</p>
          <ul className="mt-4 space-y-2 text-sm text-[#6d6a64]">
            <li><Link to="/register?role=customer">Customer registration</Link></li>
            <li><Link to="/register?role=technician">Technician registration</Link></li>
            <li><Link to="/login">Login</Link></li>
          </ul>
        </div>
        <div>
          <p className="landing-kicker text-[#171717]">Support</p>
          <ul className="mt-4 space-y-2 text-sm text-[#6d6a64]">
            <li><a href="#about">About</a></li>
            <li><a href="#contact">Contact</a></li>
            <li><a href="#privacy">Privacy</a></li>
          </ul>
        </div>
      </div>
      <div id="contact" className="sr-only">
        Contact for this academic project is handled through the FixFlow course submission.
      </div>
      <div id="privacy" className="sr-only">
        Customer addresses stay hidden until booking confirmation.
      </div>
      <div className="border-t border-black/5">
        <p className="mx-auto max-w-6xl px-4 py-5 text-xs text-[#6d6a64] sm:px-6">
          © 2026 FixFlow AI. SE3090 Software Engineering Frameworks Project.
        </p>
      </div>
    </footer>
  )
}
