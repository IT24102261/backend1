import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Navbar } from '../components/layout/Navbar'
import { Sidebar } from '../components/layout/Sidebar'

export function DashboardLayout({ variant }: { variant: 'admin' | 'technician' | 'customer' }) {
  const [open, setOpen] = useState(false)

  return (
    <div className="min-h-screen bg-[#faf7f1]">
      <div className="flex min-h-screen">
        <div className="sticky top-0 hidden h-screen lg:block">
          <Sidebar variant={variant} />
        </div>
        {open ? (
          <div className="fixed inset-0 z-40 lg:hidden">
            <button className="absolute inset-0 bg-slate-950/50 backdrop-blur-sm" onClick={() => setOpen(false)} aria-label="Close menu" />
            <div className="relative h-full w-64 overflow-hidden rounded-r-3xl shadow-2xl">
              <Sidebar variant={variant} onNavigate={() => setOpen(false)} />
            </div>
          </div>
        ) : null}
        <div className="flex min-w-0 flex-1 flex-col">
          <Navbar onMenu={() => setOpen(true)} />
          <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  )
}
