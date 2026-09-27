import { useEffect, useState, type FormEvent } from 'react'
import { authApi } from '../../api/auth'
import { Button } from '../../components/ui/Button'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, TextInput } from '../../components/ui/FormField'
import { PageHeader } from '../../components/ui/PageHeader'
import { useAuth } from '../../hooks/useAuth'
import { tokenStorage } from '../../services/tokenStorage'
import { useToastStore } from '../../store/toastStore'
import { getApiError } from '../../utils/errors'
import { isValidPhone } from '../../utils/validation'

export function CustomerProfilePage() {
  const user = useAuth((state) => state.user)
  const hydrate = useAuth((state) => state.hydrate)
  const push = useToastStore((state) => state.push)
  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [phone, setPhone] = useState(user?.phone ?? '')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState('')
  const [phoneError, setPhoneError] = useState('')

  useEffect(() => {
    authApi
      .me()
      .then((me) => {
        setDisplayName(me.displayName)
        setPhone(me.phone ?? '')
        tokenStorage.setUser(me)
      })
      .catch((err) => setError(getApiError(err).error))
  }, [])

  async function saveProfile(event: FormEvent) {
    event.preventDefault()
    if (phone.trim() && !isValidPhone(phone)) {
      setPhoneError('Phone number must be 10 digits.')
      return
    }
    setPhoneError('')
    try {
      const me = await authApi.updateProfile({ displayName, phone })
      tokenStorage.setUser(me)
      await hydrate()
      push('success', 'Profile updated.')
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  async function savePassword(event: FormEvent) {
    event.preventDefault()
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      push('success', 'Password changed. Sign in again on other devices.')
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Profile" description="Update your account details or change your password." />
      {error ? <ErrorState message={error} /> : null}
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={saveProfile}>
        <FormField label="Display name">
          <TextInput value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
        </FormField>
        <FormField label="Phone" error={phoneError} hint="10 digits.">
          <TextInput value={phone} inputMode="numeric" maxLength={10} onChange={(event) => setPhone(event.target.value.replace(/\D/g, '').slice(0, 10))} />
        </FormField>
        <Button type="submit">Save profile</Button>
      </form>
      <form className="grid gap-4 rounded-2xl border border-black/8 bg-white p-5 md:grid-cols-2" onSubmit={savePassword}>
        <FormField label="Current password">
          <TextInput type="password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} />
        </FormField>
        <FormField label="New password">
          <TextInput type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
        </FormField>
        <Button type="submit">Change password</Button>
      </form>
    </div>
  )
}
