import { googleMapsDirectionsUrl, googleMapsEmbedUrl, googleMapsSearchUrl } from '../../api/maps'

export function LocationMap({
  address,
  latitude,
  longitude,
  title = 'Exact location',
}: {
  address?: string | null
  latitude?: number | null
  longitude?: number | null
  title?: string
}) {
  const embed = googleMapsEmbedUrl({ latitude, longitude, address })
  const search = googleMapsSearchUrl({ latitude, longitude, address })
  const directions = googleMapsDirectionsUrl({ latitude, longitude, address })

  if (!embed && !search) {
    return <p className="text-sm text-[#6d6a64]">Exact location is not available yet.</p>
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="font-medium text-[#171717]">{title}</p>
          {address ? <p className="mt-1 text-sm text-[#6d6a64]">{address}</p> : null}
          {latitude != null && longitude != null ? (
            <p className="mt-1 text-xs text-[#9a968e]">
              GPS {latitude.toFixed(6)}, {longitude.toFixed(6)}
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap gap-2">
          {search ? (
            <a
              href={search}
              target="_blank"
              rel="noreferrer"
              className="rounded-xl bg-[#171717] px-3 py-2 text-sm font-medium text-white hover:bg-[#2a2a2a]"
            >
              Open Google Maps
            </a>
          ) : null}
          {directions ? (
            <a
              href={directions}
              target="_blank"
              rel="noreferrer"
              className="rounded-xl border border-[#171717]/15 bg-white px-3 py-2 text-sm font-medium text-[#171717] hover:bg-[#f4efe6]"
            >
              GPS directions
            </a>
          ) : null}
        </div>
      </div>
      {embed ? (
        <iframe
          title={title}
          src={embed}
          className="h-72 w-full rounded-2xl border border-black/8"
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
        />
      ) : null}
    </div>
  )
}
