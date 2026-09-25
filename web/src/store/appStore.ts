import { create } from 'zustand'

type AppState = {
  apiStatus: string
  setApiStatus: (status: string) => void
}

export const useAppStore = create<AppState>((set) => ({
  apiStatus: 'unknown',
  setApiStatus: (apiStatus) => set({ apiStatus }),
}))
