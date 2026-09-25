import {
  Bot,
  Briefcase,
  ClipboardList,
  FolderTree,
  LayoutDashboard,
  MessageSquareWarning,
  ScrollText,
  ShieldCheck,
  Bell,
  BarChart3,
  Star,
  UserRound,
  Users,
  House,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'

const admin = [
  { to: '/admin/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/admin/users', label: 'Users', icon: Users },
  { to: '/admin/verifications', label: 'Verifications', icon: ShieldCheck },
  { to: '/admin/categories', label: 'Categories', icon: FolderTree },
  { to: '/admin/technicians', label: 'Technicians', icon: UserRound },
  { to: '/admin/requests', label: 'Requests', icon: ClipboardList },
  { to: '/admin/bookings', label: 'Bookings', icon: Briefcase },
  { to: '/admin/reviews', label: 'Reviews', icon: Star },
  { to: '/admin/complaints', label: 'Complaints', icon: MessageSquareWarning },
  { to: '/admin/ai-workflows', label: 'AI Workflows', icon: Bot },
  { to: '/admin/audit', label: 'Audit log', icon: ScrollText },
  { to: '/admin/reports', label: 'Reports', icon: BarChart3 },
]

const technician = [
  { to: '/technician/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/technician/verification', label: 'Verification', icon: ShieldCheck },
  { to: '/technician/invitations', label: 'Invitations', icon: ClipboardList },
  { to: '/technician/quotations', label: 'Quotations', icon: ScrollText },
  { to: '/technician/jobs', label: 'Jobs', icon: Briefcase },
  { to: '/technician/profile', label: 'Profile', icon: UserRound },
  { to: '/technician/reviews', label: 'Reviews', icon: Star },
  { to: '/technician/complaints', label: 'Complaints', icon: MessageSquareWarning },
  { to: '/technician/notifications', label: 'Notifications', icon: Bell },
]

const customer = [
  { to: '/customer/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/customer/requests', label: 'Requests', icon: ClipboardList },
  { to: '/customer/bookings', label: 'Bookings', icon: Briefcase },
  { to: '/customer/reviews', label: 'Reviews', icon: Star },
  { to: '/customer/complaints', label: 'Complaints', icon: MessageSquareWarning },
  { to: '/customer/notifications', label: 'Notifications', icon: Bell },
  { to: '/customer/profile', label: 'Profile', icon: UserRound },
]

const menus = { admin, technician, customer }

export function Sidebar({ variant, onNavigate }: { variant: keyof typeof menus; onNavigate?: () => void }) {
  return (
    <aside className="flex h-full w-64 flex-col bg-[#111318] text-white/70">
      <div className="flex items-center gap-3 px-5 py-7">
        <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-white/10 text-white">
          <House size={16} />
        </span>
        <div>
          <p className="text-sm font-semibold text-white">FixFlow AI</p>
          <p className="mt-1 text-xs capitalize text-[#c4a574]">{variant} workspace</p>
        </div>
      </div>
      <nav className="flex-1 space-y-1 overflow-y-auto px-3 pb-6">
        {menus[variant].map((item) => {
          const Icon = item.icon
          return (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={onNavigate}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${
                  isActive ? 'bg-white font-medium text-[#111318] shadow-sm' : 'hover:bg-white/10 hover:text-white'
                }`
              }
            >
              <Icon size={16} />
              {item.label}
            </NavLink>
          )
        })}
      </nav>
    </aside>
  )
}
