import { Link } from 'react-router-dom'
import { EmptyState } from '../../components/ui/EmptyState'

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-xl px-6 py-16">
      <EmptyState
        title="Page not found"
        description="That route is not part of the FixFlow web app."
        action={
          <Link to="/" className="text-sm font-medium text-[#c4a574] hover:text-[#171717]">
            Go to the landing page
          </Link>
        }
      />
    </div>
  )
}
