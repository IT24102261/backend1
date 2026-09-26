import { House, Menu } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { Button } from '../ui/Button'

export function Navbar({ onMenu }: { onMenu?: () => void }) {
  const user = useAuth((state) => state.user)
  const logout = useAuth((state) => state.logout)
  const homePath = useAuth((state) => state.homePath)
  const initials = user?.displayName
    ?.split(' ')
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()

  return (
    <header className="sticky top-0 z-30 border-b border-black/5 bg-[#faf7f1]/90 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4 sm:px-6">
        <div className="flex items-center gap-3">
          {onMenu ? (
            <button
              type="button"
              className="rounded-xl p-2 text-[#171717] transition hover:bg-[#f4efe6] lg:hidden"
              onClick={onMenu}
              aria-label="Open menu"
            >
              <Menu size={20} />
            </button>
          ) : null}
          <Link to="/" className="flex items-center gap-2.5 text-sm font-semibold text-[#171717]">
            <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-[#171717] text-white">
              <House size={14} />
            </span>
            FixFlow AI
          </Link>
        </div>
        <nav className="hidden items-center gap-4 text-sm font-medium text-[#171717] md:flex">
          {user ? (
            <>
              <Link to={homePath()} className="rounded-lg px-2 py-1 transition hover:bg-[#f4efe6] hover:text-[#9b7d4e]">
                Dashboard
              </Link>
              <span className="inline-flex items-center gap-2 rounded-full border border-black/8 bg-white py-1 pr-3 pl-1 shadow-sm">
                <span className="flex h-7 w-7 items-center justify-center rounded-full bg-[#171717] text-[11px] font-semibold text-white">
                  {initials || 'U'}
                </span>
                <span className="max-w-36 truncate">{user.displayName}</span>
              </span>
              <Button variant="secondary" onClick={() => void logout()}>
                Logout
              </Button>
            </>
          ) : (
            <>
              <Link to="/login" className="rounded-lg px-2 py-1 transition hover:bg-[#f4efe6]">
                Login
              </Link>
              <Link to="/register" className="rounded-xl bg-[#171717] px-3.5 py-2 text-white shadow-sm transition hover:bg-[#2a2a2a]">
                Register
              </Link>
            </>
          )}
        </nav>
        <div className="md:hidden">
          {user ? (
            <Button variant="secondary" onClick={() => void logout()}>
              Logout
            </Button>
          ) : (
            <Link to="/login" className="text-sm font-semibold text-[#9b7d4e]">
              Login
            </Link>
          )}
        </div>
      </div>
    </header>
  )
}
