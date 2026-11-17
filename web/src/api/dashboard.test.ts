import { createElement } from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Route, Routes } from 'react-router-dom'
import { StaticRouter } from 'react-router-dom/server'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, API_BASE_URL } from './client.ts'
import { getProjectDashboard } from './dashboard.ts'
import { ProjectDashboard as DashboardView } from '../components/ProjectDashboard.tsx'
import { DashboardError, ProjectDashboardPage } from '../pages/ProjectDashboardPage.tsx'
import { useAuthStore } from '../stores/authStore.ts'
import type { ProjectDashboard } from '../types/dashboard.ts'

const data: ProjectDashboard = {
  projectId: 'project-1',
  generatedAtUtc: '2026-11-18T01:00:00Z',
  tasks: { total: 240, todo: 80, inProgress: 40, done: 120, overdue: 8, completionPercent: 50 },
  byAssignee: [
    { assigneeId: 'member-1', displayName: 'Nguyễn An', total: 140, done: 100 },
    { assigneeId: null, displayName: 'Chưa giao', total: 100, done: 20 },
  ],
  activeSprint: {
    id: 'sprint-1', name: 'Sprint thật', startDate: '2026-11-01T00:00:00Z', endDate: null,
    totalTasks: 10, completedTasks: 4, remainingTasks: 6,
  },
}

beforeEach(() => {
  useAuthStore.setState({ tokens: null, user: null, isAuthenticated: false })
})
afterEach(() => vi.unstubAllGlobals())

describe('Dashboard API contract', () => {
  it('reads server aggregates beyond 100 tasks without fetching a task page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(data))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getProjectDashboard('project-1')).resolves.toEqual(data)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith(`${API_BASE_URL}/api/projects/project-1/dashboard`, expect.any(Object))
  })

  it.each([401, 403, 404, 503])('propagates HTTP %s instead of substituting demo statistics', async (status) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ detail: 'Dashboard unavailable' }, { status })))
    await expect(getProjectDashboard('project-1')).rejects.toMatchObject({ status, message: 'Dashboard unavailable' })
  })
})

describe('Dashboard display', () => {
  it('renders full totals, the unassigned group and the actual active sprint', () => {
    const html = renderToStaticMarkup(createElement(DashboardView, { data }))
    expect(html).toContain('>240</dd>')
    expect(html).toContain('>Chưa giao</th>')
    expect(html).toContain('Nguyễn An')
    expect(html).toContain('value="50"')
    expect(html).toContain('value="40"')
    expect(html).toContain('Sprint thật')
    expect(html).toContain('Chưa đặt')
    expect(html).not.toContain('Demo')
  })

  it('renders an empty project and a missing sprint without NaN or fabricated sprint data', () => {
    const html = renderToStaticMarkup(createElement(DashboardView, { data: {
      ...data,
      tasks: { total: 0, todo: 0, inProgress: 0, done: 0, overdue: 0, completionPercent: 0 },
      byAssignee: [], activeSprint: null,
    } }))
    expect(html).toContain('Dự án chưa có công việc')
    expect(html).toContain('Chưa có Sprint đang chạy')
    expect(html).toContain('Chưa có dữ liệu phân bổ')
    expect(html).not.toMatch(/NaN|Infinity|Sprint 4/)
  })

  it('handles an active sprint with zero tasks', () => {
    const html = renderToStaticMarkup(createElement(DashboardView, { data: {
      ...data, activeSprint: { ...data.activeSprint!, totalTasks: 0, completedTasks: 0, remainingTasks: 0 },
      byAssignee: [{ assigneeId: 'member-1', displayName: 'Nguyễn An', total: 0, done: 0 }],
    } }))
    expect(html).not.toMatch(/NaN|Infinity/)
    expect(html).toContain('value="0"')
  })

  it.each([
    [403, 'Bạn không có quyền xem báo cáo dự án này'],
    [404, 'Không tìm thấy dự án'],
    [503, 'Không thể tải báo cáo'],
  ])('explains HTTP %s and provides a retry action', (status, title) => {
    const html = renderToStaticMarkup(createElement(DashboardError, { error: new ApiError(status as number, 'Chi tiết lỗi'), retry: vi.fn() }))
    expect(html).toContain(title)
    expect(html).toContain('Thử lại')
    expect(html).toContain('role="alert"')
  })

  function renderRoute(client: QueryClient) {
    return renderToStaticMarkup(createElement(QueryClientProvider, { client },
      createElement(StaticRouter, { location: '/projects/project-1/dashboard' },
        createElement(Routes, null, createElement(Route, {
          path: '/projects/:projectId/dashboard', element: createElement(ProjectDashboardPage),
        }))),
    ))
  }

  it('loads the project-specific report at its own route', () => {
    const client = new QueryClient()
    try {
      client.setQueryData(['project', 'project-1'], { name: 'Dự án A' })
      client.setQueryData(['dashboard', 'project-1'], data)
      const html = renderRoute(client)
      expect(html).toContain('Tổng quan dự án · Dự án A')
      expect(html).toContain('>240</dd>')
      expect(html).toContain('href="/projects/project-1"')
    } finally { client.clear() }
  })

  it('hides cached statistics when a later refresh fails', () => {
    const client = new QueryClient()
    try {
      client.setQueryData(['project', 'project-1'], { name: 'Dự án A' })
      client.setQueryData(['dashboard', 'project-1'], data)
      client.getQueryCache().find({ queryKey: ['dashboard', 'project-1'] })!.setState({
        status: 'error', error: new ApiError(503, 'Dashboard unavailable'),
      })
      const html = renderRoute(client)
      expect(html).toContain('Không thể tải báo cáo')
      expect(html).not.toContain('>240</dd>')
      expect(html).not.toContain('Cập nhật lúc')
    } finally { client.clear() }
  })
})
