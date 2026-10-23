import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthTokens, UserSummary } from '../types/auth.ts'

interface AuthState {
  tokens: AuthTokens | null
  user: UserSummary | null
  isAuthenticated: boolean
  setAuth: (tokens: AuthTokens, user?: UserSummary | null) => void
  setUser: (user: UserSummary) => void
  logout: () => void
}

// Giải mã payload từ JWT để lấy thông tin cơ bản người dùng nếu có
function decodeJwt(token: string): Partial<UserSummary> | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = JSON.parse(atob(parts[1]))
    return {
      id: payload.sub || payload.nameid || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '',
      email: payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '',
      displayName: payload.name || payload.unique_name || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '',
    }
  } catch {
    return null
  }
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      tokens: null,
      user: null,
      isAuthenticated: false,

      setAuth: (tokens, user) => {
        let inferredUser = user
        if (!inferredUser) {
          const decoded = decodeJwt(tokens.accessToken)
          if (decoded && decoded.id) {
            inferredUser = {
              id: decoded.id,
              email: decoded.email || '',
              displayName: decoded.displayName || decoded.email || 'Người dùng',
            }
          }
        }
        set({
          tokens,
          user: inferredUser ?? null,
          isAuthenticated: true,
        })
      },

      setUser: (user) => set({ user }),

      logout: () => {
        set({
          tokens: null,
          user: null,
          isAuthenticated: false,
        })
      },
    }),
    {
      name: 'pmai_auth_storage',
      partialize: (state) => ({ tokens: state.tokens, user: state.user, isAuthenticated: state.isAuthenticated }),
    },
  ),
)
