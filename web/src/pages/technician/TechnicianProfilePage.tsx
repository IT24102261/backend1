import { useEffect, useState, type FormEvent } from 'react'
import { techniciansApi } from '../../api/technicians'
import { Button } from '../../components/ui/Button'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, TextArea, TextInput } from '../../components/ui/FormField'
import { LoadingSpinner } from '../../components/ui/LoadingSpinner'
import { PageHeader } from '../../components/ui/PageHeader'
import { useToastStore } from '../../store/toastStore'
import type { TechnicianProfileDto } from '../../types/api'
import { getApiError } from '../../utils/errors'

export function TechnicianProfilePage() {
  const push = useToastStore((state) => state.push)
  const [profile, setProfile] = useState<TechnicianProfileDto | null>(null)
  const [bio, setBio] = useState('')
  const [serviceArea, setServiceArea] = useState('')
  const [experience, setExperience] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    techniciansApi
      .getProfile()
      .then((data) => {
        setProfile(data)
        setBio(data.bio ?? '')
        setServiceArea(data.serviceArea ?? '')
        setExperience(data.experienceSummary ?? '')
      })
      .catch((err) => setError(getApiError(err).error))
      .finally(() => setLoading(false))
  }, [])

  async function save(event: FormEvent) {
    event.preventDefault()
    const payload = { bio, serviceArea, experienceSummary: experience }
    try {
      const saved = profile
        ? await techniciansApi.updateProfile(payload)
        : await techniciansApi.createProfile(payload)
      setProfile(saved)
      push('success', 'Profile saved.')
    } catch (err) {
      push('error', getApiError(err).error)
    }
  }

  if (loading) return <LoadingSpinner label="Loading profile" />

  return (
    <div className="space-y-5">
      <PageHeader title="Profile" description="Service area is shown to customers before booking. Exact addresses stay hidden." />
      {error ? <ErrorState message={error} /> : null}
      <form className="space-y-4 rounded-2xl border border-black/8 bg-white p-5" onSubmit={save}>
        <FormField label="Bio">
          <TextArea value={bio} onChange={(event) => setBio(event.target.value)} />
        </FormField>
        <FormField label="Service area">
          <TextInput value={serviceArea} onChange={(event) => setServiceArea(event.target.value)} />
        </FormField>
        <FormField label="Experience">
          <TextArea value={experience} onChange={(event) => setExperience(event.target.value)} />
        </FormField>
        {profile ? (
          <p className="text-sm text-slate-500">
            Approved categories: {profile.approvedCategories.join(', ') || 'none yet'}
          </p>
        ) : null}
        <Button type="submit">{profile ? 'Update profile' : 'Create profile'}</Button>
      </form>
    </div>
  )
}
