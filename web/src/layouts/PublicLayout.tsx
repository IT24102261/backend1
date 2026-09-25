import { Outlet, useLocation } from 'react-router-dom'
import { Navbar } from '../components/layout/Navbar'

export function PublicLayout() {
  const { pathname } = useLocation()
  const isLanding = pathname === '/'

  return (
    <div className="min-h-screen bg-[#faf7f1]">
      {isLanding ? null : <Navbar />}
      <main>
        <Outlet />
      </main>
    </div>
  )
}
