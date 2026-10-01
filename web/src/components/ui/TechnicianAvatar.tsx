import { useEffect, useState } from 'react'
import { mediaUrl } from '../../utils/media'

export function TechnicianAvatar({
  name,
  photoUrl,
  size = 48,
}: {
  name?: string | null
  photoUrl?: string | null
  size?: number
}) {
  const src = mediaUrl(photoUrl)
  const [failed, setFailed] = useState(false)
  const initials = (name ?? 'T')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')

  useEffect(() => {
    setFailed(false)
  }, [src])

  if (src && !failed) {
    return (
      <img
        src={src}
        alt={name || 'Technician'}
        width={size}
        height={size}
        className="shrink-0 rounded-full object-cover"
        style={{ width: size, height: size }}
        onError={() => setFailed(true)}
      />
    )
  }

  return (
    <span
      className="inline-flex shrink-0 items-center justify-center rounded-full bg-[#f4efe6] text-sm font-semibold text-[#171717]"
      style={{ width: size, height: size }}
    >
      {initials || 'T'}
    </span>
  )
}
