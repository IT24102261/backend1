import { useEffect, useRef, useState, type FormEvent } from 'react'
import { techniciansApi } from '../../api/technicians'
import { Button } from '../../components/ui/Button'
import { ErrorState } from '../../components/ui/ErrorState'
import { FormField, TextArea, TextInput } from '../../components/ui/FormField'
import { LoadingSpinner } from '../../components/ui/LoadingSpinner'
import { PageHeader } from '../../components/ui/PageHeader'
import { StarRating } from '../../components/ui/StarRating'
import { TechnicianAvatar } from '../../components/ui/TechnicianAvatar'
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
  const [savingPhoto, setSavingPhoto] = useState(false)
  const photoInput = useRef<HTMLInputElement>(null)

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

  async function changePhoto(file?: File) {
    if (!file) return
    setSavingPhoto(true)
    try {
      const updated = await techniciansApi.updateOwnPhoto(file)
      setProfile(updated)
      push('success', 'Profile photo updated.')
    } catch (err) {
      push('error', getApiError(err).error)
    } finally {
      setSavingPhoto(false)
    }
  }

  if (loading) return <LoadingSpinner label="Loading profile" />

  return (
    <div className="space-y-5">
      <PageHeader title="Profile" description="Service area is shown to customers before booking. Exact addresses stay hidden." />
      {error ? <ErrorState message={error} /> : null}
      <form className="space-y-4 rounded-2xl border border-black/8 bg-white p-5" onSubmit={save}>
        <div className="flex flex-wrap items-center gap-4">
          <TechnicianAvatar name={profile?.displayName} photoUrl={profile?.profilePhotoUrl} size={88} />
          <div className="space-y-2">
            <p className="text-sm font-medium text-[#171717]">Profile photo</p>
            <p className="max-w-md text-sm text-slate-500">
              Customers see this picture on quotations and bookings. A photo set by an admin appears here, and you can replace it.
            </p>
            <input
              ref={photoInput}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={(event) => {
                void changePhoto(event.target.files?.[0])
                event.target.value = ''
              }}
            />
            <Button
              type="button"
              variant="secondary"
              disabled={!profile || savingPhoto}
              onClick={() => photoInput.current?.click()}
            >
              {savingPhoto ? 'Saving…' : profile?.profilePhotoUrl ? 'Change photo' : 'Add photo'}
            </Button>
          </div>
        </div>
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
          <div className="space-y-2 rounded-xl bg-[#faf7f1] px-4 py-3">
            <p className="text-sm font-medium text-[#171717]">Average review</p>
            <StarRating value={Number(profile.averageRating)} count={profile.reviewCount} />
            <p className="text-sm text-slate-500">
              Approved categories: {profile.approvedCategories.join(', ') || 'none yet'}
            </p>
          </div>
        ) : null}
        <Button type="submit">{profile ? 'Update profile' : 'Create profile'}</Button>
      </form>
    </div>
  )
}
