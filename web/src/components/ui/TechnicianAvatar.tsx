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
  const initials = (name ?? 'T')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')

  if (src) {
    return (
      <img
        src={src}
        alt={name || 'Technician'}
        width={size}
        height={size}
        className="shrink-0 rounded-full object-cover"
        style={{ width: size, height: size }}
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
