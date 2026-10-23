import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { AuthTokens, LoginRequest, RegisterRequest } from '../types/auth.ts'

export async function login(data: LoginRequest): Promise<AuthTokens> {
  if (IS_MOCK) {
    return mockService.login(data.email).tokens
  }

  return apiClient<AuthTokens>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function register(data: RegisterRequest): Promise<{ id: string }> {
  if (IS_MOCK) {
    return { id: `mock-user-${Date.now()}` }
  }

  return apiClient<{ id: string }>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function refresh(refreshToken: string): Promise<AuthTokens> {
  return apiClient<AuthTokens>('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refreshToken }),
  })
}
