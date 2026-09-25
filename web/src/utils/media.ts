const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080'

export function mediaUrl(path?: string | null) {
  if (!path) return null
  if (path.startsWith('http://') || path.startsWith('https://')) return path
  return `${apiBase}${path.startsWith('/') ? path : `/${path}`}`
}
