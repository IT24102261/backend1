import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { UserPlus } from 'lucide-react'
import { categoriesApi } from '../api/categories'
import { Button } from '../components/ui/Button'
import { ErrorState } from '../components/ui/ErrorState'
import { FormField, SelectInput, TextInput } from '../components/ui/FormField'
import { useAuth } from '../hooks/useAuth'
import { pathForRole } from '../store/authStore'
import { useToastStore } from '../store/toastStore'
import type { CategoryDto } from '../types/api'
import { getApiError } from '../utils/errors'

export function RegisterPage() {
  const register = useAuth((state) => state.register)
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const push = useToastStore((state) => state.push)
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [nicPhoto, setNicPhoto] = useState<File | null>(null)
  const [certificate, setCertificate] = useState<File | null>(null)
  const [profilePhoto, setProfilePhoto] = useState<File | null>(null)
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'CUSTOMER' | 'TECHNICIAN'>(
    params.get('role')?.toUpperCase() === 'TECHNICIAN' ? 'TECHNICIAN' : 'CUSTOMER',
  )
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    categoriesApi
      .list()
      .then((items) => setCategories(items.filter((item) => item.isActive)))
      .catch(() => undefined)
  }, [])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const next: Record<string, string> = {}
    if (displayName.trim().length < 2) next.displayName = 'Enter your full name.'
    if (!email.includes('@')) next.email = 'Enter a valid email.'
    if (password.length < 8) next.password = 'Use at least 8 characters.'
    const selectedTrade = categories.find((item) => item.id === categoryId)
    const electricianSelected = selectedTrade?.name.toLowerCase() === 'electrician'
    if (role === 'TECHNICIAN') {
      if (!phone.trim()) next.phone = 'Enter your phone number.'
      if (!address.trim()) next.address = 'Enter your address.'
      if (!categoryId) next.categoryId = 'Select the field you work in.'
      if (!nicPhoto) next.nicPhoto = 'Upload a photo of your NIC.'
      if (!profilePhoto) next.profilePhoto = 'Upload a profile photo.'
      if (electricianSelected && !certificate) next.certificate = 'Electricians must upload their studied certificate.'
    }
    setErrors(next)
    if (Object.keys(next).length) return

    setBusy(true)
    setFormError('')
    try {
      const result = await register({
        displayName,
        email,
        password,
        phone: phone || undefined,
        address: role === 'TECHNICIAN' ? address : undefined,
        categoryId: role === 'TECHNICIAN' ? categoryId : undefined,
        nicPhoto: role === 'TECHNICIAN' ? nicPhoto ?? undefined : undefined,
        certificate: role === 'TECHNICIAN' ? certificate ?? undefined : undefined,
        profilePhoto: role === 'TECHNICIAN' ? profilePhoto ?? undefined : undefined,
        role,
      })
      if (result.requiresAdminApproval) {
        push('success', 'Registration received. An admin must approve your login before you can sign in.')
        navigate('/login', { replace: true })
        return
      }
      push('success', 'Account created.')
      navigate(pathForRole(result.user?.role), { replace: true })
    } catch (error) {
      setFormError(getApiError(error).error)
    } finally {
      setBusy(false)
    }
  }

  const electricianSelected = categories.find((item) => item.id === categoryId)?.name.toLowerCase() === 'electrician'

  return (
    <section className="mx-auto max-w-md px-6 py-16">
      <div className="rounded-3xl border border-black/8 bg-white p-8 shadow-[var(--shadow-card)]">
        <span className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-[#f4efe6] text-[#171717]">
          <UserPlus size={18} />
        </span>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight text-[#171717]">Create an account</h1>
        <p className="mt-2 text-sm leading-6 text-[#6d6a64]">
          {role === 'TECHNICIAN'
            ? 'Technician accounts are reviewed by an admin before login is allowed. Your selected trade is saved for verification.'
            : 'Create a customer account to request home services.'}
        </p>
        {formError ? (
          <div className="mt-5">
            <ErrorState title="Registration failed" message={formError} />
          </div>
        ) : null}
        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <FormField label="I am a">
            <SelectInput value={role} onChange={(event) => setRole(event.target.value as 'CUSTOMER' | 'TECHNICIAN')}>
              <option value="CUSTOMER">Customer looking for a technician</option>
              <option value="TECHNICIAN">Technician offering services</option>
            </SelectInput>
          </FormField>
          <FormField label="Full name" error={errors.displayName}>
            <TextInput value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
          </FormField>
          <FormField label="Email" error={errors.email}>
            <TextInput type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
          </FormField>
          <FormField label="Phone" error={errors.phone}>
            <TextInput value={phone} onChange={(event) => setPhone(event.target.value)} />
          </FormField>
          {role === 'TECHNICIAN' ? (
            <>
              <FormField label="Address" error={errors.address}>
                <TextInput value={address} onChange={(event) => setAddress(event.target.value)} placeholder="Street, town, district" />
              </FormField>
              <FormField label="Field you work in" error={errors.categoryId} hint="This is saved against your technician profile. Admin still reviews login and category approval.">
                <SelectInput value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
                  <option value="">Select a trade</option>
                  {categories.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))}
                </SelectInput>
              </FormField>
              <FormField label="Profile photo" error={errors.profilePhoto} hint="Required. Customers see this when they book you from a quotation.">
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={(event) => setProfilePhoto(event.target.files?.[0] ?? null)}
                  className="block w-full text-sm text-[#6d6a64] file:mr-3 file:rounded-lg file:border-0 file:bg-[#f4efe6] file:px-3 file:py-2 file:font-medium file:text-[#171717]"
                />
              </FormField>
              <FormField label="NIC photo" error={errors.nicPhoto} hint="Required for every technician. JPEG, PNG or WebP, up to 5 MB.">
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={(event) => setNicPhoto(event.target.files?.[0] ?? null)}
                  className="block w-full text-sm text-[#6d6a64] file:mr-3 file:rounded-lg file:border-0 file:bg-[#f4efe6] file:px-3 file:py-2 file:font-medium file:text-[#171717]"
                />
              </FormField>
              <FormField
                label={electricianSelected ? 'Studied certificate' : 'Studied certificate (optional)'}
                error={errors.certificate}
                hint={
                  electricianSelected
                    ? 'Required for electricians. Upload your trade or study certificate (image or PDF).'
                    : 'Optional for this trade. You can still attach a certificate if you have one.'
                }
              >
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  onChange={(event) => setCertificate(event.target.files?.[0] ?? null)}
                  className="block w-full text-sm text-[#6d6a64] file:mr-3 file:rounded-lg file:border-0 file:bg-[#f4efe6] file:px-3 file:py-2 file:font-medium file:text-[#171717]"
                />
              </FormField>
            </>
          ) : null}
          <FormField label="Password" error={errors.password} hint="Minimum 8 characters.">
            <TextInput type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
          </FormField>
          <Button type="submit" disabled={busy} className="w-full">
            {busy ? 'Creating account…' : 'Register'}
          </Button>
        </form>
        <p className="mt-5 text-sm text-[#6d6a64]">
          Already registered?{' '}
          <Link to="/login" className="font-semibold text-[#c4a574] hover:text-[#171717]">
            Sign in
          </Link>
        </p>
      </div>
    </section>
  )
}
