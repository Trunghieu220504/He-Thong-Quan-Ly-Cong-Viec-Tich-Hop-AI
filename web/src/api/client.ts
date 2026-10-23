import { useAuthStore } from '../stores/authStore.ts'
import type { AuthTokens, ProblemDetails } from '../types/auth.ts'

export const API_BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) || 'http://localhost:5046'
export const IS_MOCK = (import.meta.env.VITE_USE_MOCK as string | undefined) === 'true'

export class ApiError extends Error {
  status: number
  problemDetails?: ProblemDetails

  constructor(status: number, message: string, problemDetails?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problemDetails = problemDetails
  }
}

let isRefreshing = false
let refreshSubscribers: ((token: string | null) => void)[] = []

function onRefreshed(token: string | null) {
  refreshSubscribers.forEach((callback) => callback(token))
  refreshSubscribers = []
}

function addRefreshSubscriber(callback: (token: string | null) => void) {
  refreshSubscribers.push(callback)
}

async function requestRefreshToken(): Promise<string | null> {
  const { tokens, setAuth, logout } = useAuthStore.getState()
  if (!tokens?.refreshToken) {
    logout()
    return null
  }

  try {
    const res = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: tokens.refreshToken }),
    })

    if (!res.ok) {
      logout()
      return null
    }

    const data = (await res.json()) as AuthTokens
    setAuth(data)
    return data.accessToken
  } catch {
    logout()
    return null
  }
}

export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {},
  isRetry = false,
): Promise<T> {
  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}${endpoint}`
  const headers = new Headers(options.headers || {})

  if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const { tokens, logout } = useAuthStore.getState()
  if (tokens?.accessToken && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${tokens.accessToken}`)
  }

  let response: Response
  try {
    response = await fetch(url, {
      ...options,
      headers,
    })
  } catch (networkError) {
    const message = networkError instanceof Error ? networkError.message : 'Lỗi kết nối mạng'
    throw new ApiError(0, `Không thể kết nối đến máy chủ: ${message}`)
  }

  if (response.status === 401 && !isRetry && tokens?.refreshToken && !endpoint.includes('/api/auth/')) {
    if (!isRefreshing) {
      isRefreshing = true
      const newToken = await requestRefreshToken()
      isRefreshing = false
      onRefreshed(newToken)

      if (newToken) {
        return apiClient<T>(endpoint, options, true)
      }
    } else {
      return new Promise<T>((resolve, reject) => {
        addRefreshSubscriber((newToken) => {
          if (newToken) {
            resolve(apiClient<T>(endpoint, options, true))
          } else {
            reject(new ApiError(401, 'Phiên đăng nhập đã hết hạn.'))
          }
        })
      })
    }
  }

  if (!response.ok) {
    let errorDetail = `Lỗi hệ thống (${response.status})`
    let problem: ProblemDetails | undefined

    try {
      problem = (await response.json()) as ProblemDetails
      if (problem.detail) {
        errorDetail = problem.detail
      } else if (problem.title) {
        errorDetail = problem.title
      } else if (problem.errors) {
        const firstErrorKey = Object.keys(problem.errors)[0]
        if (firstErrorKey && problem.errors[firstErrorKey]?.[0]) {
          errorDetail = problem.errors[firstErrorKey][0]
        }
      }
    } catch {
      // Body không phải JSON (ví dụ 502/504 HTML)
    }

    if (response.status === 401) {
      logout()
    }

    throw new ApiError(response.status, errorDetail, problem)
  }

  if (response.status === 204) {
    return {} as T
  }

  return response.json() as Promise<T>
}
