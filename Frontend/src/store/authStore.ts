import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  accessToken: string | null
  role: 'customer' | 'admin' | null
  name: string | null
  setAuth: (token: string, role: 'customer' | 'admin', name: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      role: null,
      name: null,
      setAuth: (token, role, name) => set({ accessToken: token, role, name }),
      logout: () => set({ accessToken: null, role: null, name: null }),
    }),
    { name: 'devolvefacill-auth' }
  )
)
