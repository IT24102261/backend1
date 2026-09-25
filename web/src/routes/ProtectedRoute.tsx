import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { LoadingSpinner } from '../components/ui/LoadingSpinner'
import { useAuth } from '../hooks/useAuth'

export function ProtectedRoute() {
  const ready = useAuth((state) => state.ready)
  const user = useAuth((state) => state.user)
  const location = useLocation()

  if (!ready) return <LoadingSpinner label="Checking your session" />
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Outlet />
}
