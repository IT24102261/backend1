import axios from 'axios'
import type { ApiError } from '../types/api'

export function getApiError(error: unknown): ApiError {
  if (axios.isAxiosError<ApiError>(error)) {
    const data = error.response?.data
    if (data?.error) {
      return {
        error: data.error,
        code: data.code ?? 'REQUEST_FAILED',
        details: data.details,
      }
    }
    if (error.response?.status === 401) {
      return { error: 'You need to sign in to continue.', code: 'UNAUTHORIZED' }
    }
    if (error.response?.status === 403) {
      return { error: 'You are not allowed to view this page.', code: 'FORBIDDEN' }
    }
    if (error.code === 'ERR_NETWORK') {
      return { error: 'Cannot reach the FixFlow API. Check that the backend is running.', code: 'NETWORK' }
    }
  }

  if (error instanceof Error && error.message) {
    return { error: error.message, code: 'CLIENT_ERROR' }
  }

  return { error: 'Something went wrong. Please try again.', code: 'UNKNOWN' }
}
