import { useEffect, type ReactNode } from 'react'
import { useAuthStore } from '../store/authStore'

export function AuthProvider({ children }: { children: ReactNode }) {
  const hydrate = useAuthStore((state) => state.hydrate)

  useEffect(() => {
    void hydrate()
  }, [hydrate])

  return children
}
