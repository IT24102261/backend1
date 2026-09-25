import { useState } from 'react'
import { Link } from 'react-router-dom'
import { House, Menu, X } from 'lucide-react'

const links = [
  { href: '#home', label: 'Home' },
  { href: '#services', label: 'Services' },
  { href: '#how-it-works', label: 'How it works' },
  { href: '#for-technicians', label: 'Technicians' },
  { href: '#about', label: 'About' },
]

export function LandingNavbar() {
  const [open, setOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 border-b border-black/5 bg-[#faf7f1]/95 backdrop-blur-md">
      <div className="mx-auto flex h-20 max-w-6xl items-center justify-between px-4 sm:px-6">
        <a href="#home" className="flex items-center gap-2.5 text-sm font-semibold text-[#171717]">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#171717] text-white">
            <House size={16} aria-hidden="true" />
          </span>
          FixFlow AI
        </a>

        <nav className="hidden items-center gap-6 text-sm font-medium text-[#171717] lg:flex" aria-label="Landing">
          {links.map((link) => (
            <a key={link.href} href={link.href} className="rounded-lg px-2 py-1 transition hover:bg-[#f4efe6] hover:text-[#9b7d4e]">
              {link.label}
            </a>
          ))}
        </nav>

        <div className="hidden items-center gap-3 lg:flex">
          <Link to="/login" className="landing-btn landing-btn-light">
            Login
          </Link>
          <Link to="/register" className="landing-btn landing-btn-dark">
            Get started
          </Link>
        </div>

        <button
          type="button"
          className="rounded-full p-2 text-[#171717] lg:hidden"
          aria-expanded={open}
          aria-controls="landing-mobile-nav"
          aria-label={open ? 'Close menu' : 'Open menu'}
          onClick={() => setOpen((value) => !value)}
        >
          {open ? <X size={20} /> : <Menu size={20} />}
        </button>
      </div>

      {open ? (
        <div id="landing-mobile-nav" className="border-t border-black/5 bg-[#faf7f1] px-4 py-5 lg:hidden">
          <nav className="grid gap-1 text-sm" aria-label="Mobile landing">
            {links.map((link) => (
              <a key={link.href} href={link.href} className="px-2 py-2 text-[#171717]" onClick={() => setOpen(false)}>
                {link.label}
              </a>
            ))}
          </nav>
          <div className="mt-4 grid gap-2">
            <Link to="/login" className="landing-btn landing-btn-light" onClick={() => setOpen(false)}>
              Login
            </Link>
            <Link to="/register" className="landing-btn landing-btn-dark" onClick={() => setOpen(false)}>
              Get started
            </Link>
          </div>
        </div>
      ) : null}
    </header>
  )
}
