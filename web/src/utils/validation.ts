export function isValidEmail(value: string) {
  const email = value.trim()
  if (!email || email.split('@').length !== 2) return false
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
}

export function phoneDigits(value: string) {
  return value.replace(/\D/g, '')
}

export function isValidPhone(value: string) {
  return phoneDigits(value).length === 10
}

export function isFutureDateTime(value: string) {
  if (!value) return false
  const parsed = new Date(value)
  return !Number.isNaN(parsed.getTime()) && parsed.getTime() > Date.now()
}

export function datetimeLocalMin() {
  const now = new Date()
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset())
  return now.toISOString().slice(0, 16)
}
