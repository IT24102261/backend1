import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Eye, EyeOff, House } from 'lucide-react'
import { FormField, TextInput } from '../components/ui/FormField'
import { Button } from '../components/ui/Button'
import { ErrorState } from '../components/ui/ErrorState'
import { useAuth } from '../hooks/useAuth'
import { pathForRole, postLoginPath } from '../store/authStore'
import { getApiError } from '../utils/errors'
import { isValidEmail } from '../utils/validation'
import { useToastStore } from '../store/toastStore'

export function LoginPage() {
  const login = useAuth((state) => state.login)
  const user = useAuth((state) => state.user)
  const navigate = useNavigate()
  const location = useLocation()
  const push = useToastStore((state) => state.push)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState('')
  const [busy, setBusy] = useState(false)

  if (user) {
    return <Navigate to={pathForRole(user.role)} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors: Record<string, string> = {}
    if (!isValidEmail(email)) nextErrors.email = 'Enter a valid email with one @.'
    if (!password) nextErrors.password = 'Password is required.'
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    setBusy(true)
    setFormError('')
    try {
      const nextUser = await login({ email, password })
      push('success', `Signed in as ${nextUser.email}`)
      const from = (location.state as { from?: string } | null)?.from
      navigate(postLoginPath(nextUser.role, from), { replace: true })
    } catch (error) {
      setFormError(getApiError(error).error)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="mx-auto max-w-md px-6 py-16">
      <div className="rounded-3xl border border-black/8 bg-white p-8 shadow-[var(--shadow-card)]">
        <Link to="/" className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-[#f4efe6] text-[#171717]" aria-label="FixFlow home">
          <House size={18} />
        </Link>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight text-[#171717]">Sign in</h1>
        <p className="mt-2 text-sm leading-6 text-[#6d6a64]">
          Use your FixFlow account. Authorization is always enforced by the API.
        </p>
        {formError ? (
          <div className="mt-5">
            <ErrorState title="Sign in failed" message={formError} />
          </div>
        ) : null}
        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <FormField label="Email" error={errors.email}>
            <TextInput type="email" value={email} autoComplete="email" onChange={(event) => setEmail(event.target.value)} />
          </FormField>
          <FormField label="Password" error={errors.password}>
            <div className="relative">
              <TextInput
                type={showPassword ? 'text' : 'password'}
                value={password}
                autoComplete="current-password"
                className="pr-11"
                onChange={(event) => setPassword(event.target.value)}
              />
              <button
                type="button"
                className="absolute top-1/2 right-2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-lg text-[#6d6a64] hover:bg-[#f4efe6] hover:text-[#171717]"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                onClick={() => setShowPassword((current) => !current)}
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </FormField>
          <Button type="submit" disabled={busy} className="w-full">
            {busy ? 'Signing in…' : 'Sign in'}
          </Button>
        </form>
        <p className="mt-5 text-sm text-[#6d6a64]">
          New here?{' '}
          <Link to="/register" className="font-semibold text-[#c4a574] hover:text-[#171717]">
            Create an account
          </Link>
        </p>
      </div>
    </section>
  )
}
