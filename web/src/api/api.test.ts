import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient, type ApiError } from './client.ts'
import { login } from './auth.ts'
import { getProjects } from './projects.ts'
import { createTask, getTasks, updateTask, updateTaskStatus } from './tasks.ts'
import { getTaskRisk } from './risk.ts'
import { useAuthStore } from '../stores/authStore.ts'

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response
}

describe('web API client', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    useAuthStore.setState({
      tokens: {
        accessToken: 'access-token',
        accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
        refreshToken: 'refresh-token',
        refreshTokenExpiresAtUtc: '2099-01-02T00:00:00.000Z',
      },
      isAuthenticated: true,
    })
  })

  it('adds the bearer token and returns JSON responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ok: true }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiClient<{ ok: boolean }>('/api/projects')).resolves.toEqual({ ok: true })

    const request = fetchMock.mock.calls[0]?.[1] as RequestInit
    expect((request.headers as Headers).get('Authorization')).toBe('Bearer access-token')
    expect((request.headers as Headers).get('Content-Type')).toBe('application/json')
  })

  it('exposes ProblemDetails.detail through ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse({ detail: 'Không có quyền truy cập' }, 403)))

    const request = apiClient('/api/projects/private')
    await expect(request).rejects.toMatchObject({
      name: 'ApiError',
      status: 403,
      message: 'Không có quyền truy cập',
    } satisfies Partial<ApiError>)
  })
})

describe('API modules', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    useAuthStore.setState({ tokens: null, user: null, isAuthenticated: false })
  })

  it('posts login credentials to the auth endpoint', async () => {
    const tokens = {
      accessToken: 'new-access',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
      refreshToken: 'new-refresh',
      refreshTokenExpiresAtUtc: '2099-01-02T00:00:00.000Z',
    }
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(tokens))
    vi.stubGlobal('fetch', fetchMock)

    await expect(login({ email: 'baoanh@example.com', password: 'Password123!' })).resolves.toEqual(tokens)

    expect(fetchMock.mock.calls[0]?.[0]).toContain('/api/auth/login')
    expect(JSON.parse((fetchMock.mock.calls[0][1] as RequestInit).body as string)).toEqual({
      email: 'baoanh@example.com',
      password: 'Password123!',
    })
  })

  it('loads projects and unwraps paged task responses', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ items: [], page: 2, pageSize: 5, totalCount: 0 }))
      .mockResolvedValueOnce(jsonResponse({ items: [{ id: 'task-1', title: 'Test', status: 'Todo', priority: 'Low' }], page: 1, pageSize: 100, totalCount: 1 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getProjects(2, 5)).resolves.toMatchObject({ page: 2, pageSize: 5 })
    await expect(getTasks('project-1')).resolves.toHaveLength(1)
    expect(fetchMock.mock.calls[1]?.[0]).toContain('/api/projects/project-1/tasks?page=1&pageSize=100')
  })

  it('sends task creation and status updates to their endpoints', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ id: 'task-1' }))
    vi.stubGlobal('fetch', fetchMock)

    await createTask('project-1', { title: 'Write tests', priority: 'Low' })
    await updateTaskStatus('task-1', 'Done')

    expect(fetchMock.mock.calls[0]?.[0]).toContain('/api/projects/project-1/tasks')
    expect((fetchMock.mock.calls[0][1] as RequestInit).method).toBe('POST')
    expect(fetchMock.mock.calls[1]?.[0]).toContain('/api/tasks/task-1/status')
    expect(JSON.parse((fetchMock.mock.calls[1][1] as RequestInit).body as string)).toEqual({ status: 'Done' })
  })

  it('sends all six task detail fields through PUT before status and assignee patches', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({
        title: 'Old', description: 'Old description', priority: 'Low',
        dueDate: null, estimatedHours: 12, progressPercent: 45,
        status: 'Todo', assigneeId: 'user-1',
      }))
      .mockResolvedValue(jsonResponse(undefined, 204))
    vi.stubGlobal('fetch', fetchMock)

    const details = {
      title: 'Updated title', description: 'Updated description', priority: 'High' as const,
      dueDate: '2026-12-01T00:00:00Z', estimatedHours: 8, progressPercent: 60,
    }
    await updateTask('task-1', { ...details, status: 'InProgress', assigneeId: 'user-2' })

    expect(fetchMock).toHaveBeenCalledTimes(4)
    expect(fetchMock.mock.calls.map(([url, request]) => [url, request.method])).toEqual([
      [expect.stringMatching(/\/api\/tasks\/task-1$/), undefined],
      [expect.stringMatching(/\/api\/tasks\/task-1$/), 'PUT'],
      [expect.stringMatching(/\/api\/tasks\/task-1\/status$/), 'PATCH'],
      [expect.stringMatching(/\/api\/tasks\/task-1\/assignee$/), 'PATCH'],
    ])
    expect(JSON.parse(fetchMock.mock.calls[1][1].body)).toEqual(details)
    expect(JSON.parse(fetchMock.mock.calls[2][1].body)).toEqual({ status: 'InProgress' })
    expect(JSON.parse(fetchMock.mock.calls[3][1].body)).toEqual({ assigneeId: 'user-2' })
  })

  it('returns no risk when the AI endpoint is unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse({ detail: 'AI unavailable' }, 503)))

    await expect(getTaskRisk('task-1')).resolves.toBeNull()
  })
})
