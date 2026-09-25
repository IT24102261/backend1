import { apiClient } from './client'

export type ReverseGeocodeResult = {
  found: boolean
  displayName?: string | null
  serviceArea?: string | null
}

export const mapsApi = {
  reverse: (latitude: number, longitude: number) =>
    apiClient
      .post<ReverseGeocodeResult>('/api/maps/reverse', { latitude, longitude })
      .then((r) => r.data),
}

export function googleMapsSearchUrl(options: { latitude?: number | null; longitude?: number | null; address?: string | null }) {
  if (options.latitude != null && options.longitude != null) {
    return `https://www.google.com/maps/search/?api=1&query=${options.latitude},${options.longitude}`
  }
  if (options.address) {
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(options.address)}`
  }
  return null
}

export function googleMapsDirectionsUrl(options: { latitude?: number | null; longitude?: number | null; address?: string | null }) {
  const destination =
    options.latitude != null && options.longitude != null
      ? `${options.latitude},${options.longitude}`
      : options.address
        ? options.address
        : null
  if (!destination) return null
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`
}

export function googleMapsEmbedUrl(options: { latitude?: number | null; longitude?: number | null; address?: string | null }) {
  const query =
    options.latitude != null && options.longitude != null
      ? `${options.latitude},${options.longitude}`
      : options.address
  if (!query) return null
  return `https://maps.google.com/maps?q=${encodeURIComponent(query)}&z=16&output=embed`
}
