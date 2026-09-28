import { useEffect, useState } from 'react'
import { apiClient } from '../../api/client'

export function AuthenticatedMedia({
  src,
  alt,
  mimeType,
  className,
}: {
  src?: string | null
  alt: string
  mimeType?: string | null
  className?: string
}) {
  const [blobUrl, setBlobUrl] = useState<string>()
  const [error, setError] = useState('')

  useEffect(() => {
    if (!src) return
    let objectUrl: string | undefined
    let cancelled = false
    apiClient
      .get(src, {
        responseType: 'blob',
        headers: { Accept: '*/*', 'Content-Type': undefined },
      })
      .then((response) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(response.data)
        setBlobUrl(objectUrl)
      })
      .catch(() => {
        if (!cancelled) setError('Could not load this file.')
      })
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [src])

  if (!src) return <p className="text-sm text-[#6d6a64]">Not uploaded.</p>
  if (error) return <p className="text-sm text-red-700">{error}</p>
  if (!blobUrl) return <p className="text-sm text-[#6d6a64]">Loading file…</p>

  const pdf = (mimeType ?? '').toLowerCase().includes('pdf')
  if (pdf) {
    return <iframe title={alt} src={blobUrl} className={className ?? 'h-96 w-full rounded-xl border border-black/10 bg-white'} />
  }

  return <img src={blobUrl} alt={alt} className={className ?? 'max-h-80 w-full rounded-xl object-contain bg-[#f4efe6]'} />
}
