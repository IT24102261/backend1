import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import type { Role } from '../types/api'

export function RoleRoute({ roles }: { roles: Role[] }) {
  const user = useAuth((state) => state.user)
  if (!user) return <Navigate to="/login" replace />
  const role = String(user.role ?? '').toUpperCase() as Role
  if (!roles.includes(role)) return <Navigate to="/unauthorized" replace />
  return <Outlet />
}
