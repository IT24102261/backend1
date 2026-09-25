import { Link } from 'react-router-dom'
import { ErrorState } from '../../components/ui/ErrorState'
import { useAuth } from '../../hooks/useAuth'

export function UnauthorizedPage() {
  const homePath = useAuth((state) => state.homePath)

  return (
    <div className="mx-auto max-w-xl px-6 py-16">
      <ErrorState
        title="Unauthorized"
        message="Your account does not have access to that area. Role checks here are only for navigation — the API still authorizes every request."
        action={
          <Link to={homePath()} className="text-sm font-medium text-[#c4a574] hover:text-[#171717]">
            Back to your dashboard
          </Link>
        }
      />
    </div>
  )
}
