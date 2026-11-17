// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { ProjectDashboardPage } from '../pages/ProjectDashboardPage.tsx'

const { getProject, getProjects, getProjectDashboard } = vi.hoisted(() => ({
  getProject: vi.fn(), getProjects: vi.fn(), getProjectDashboard: vi.fn(),
}))
vi.mock('./projects.ts', () => ({ getProject, getProjects }))
vi.mock('./dashboard.ts', () => ({ getProjectDashboard }))

let container: HTMLDivElement
let root: Root
let client: QueryClient
async function flush() { await act(async () => { await new Promise((resolve) => setTimeout(resolve, 30)) }) }
async function click(label: string) {
  const button = Array.from(container.querySelectorAll('button')).find((item) => item.textContent === label)
  expect(button).toBeDefined()
  await act(async () => { button!.click() })
  await flush()
}
beforeEach(() => {
  vi.clearAllMocks()
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  container = document.createElement('div')
  document.body.append(container)
  root = createRoot(container)
  client = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } })
})
afterEach(() => { act(() => root.unmount()); client.clear(); container.remove() })
async function render(path: string) {
  await act(async () => root.render(createElement(QueryClientProvider, { client },
    createElement(MemoryRouter, { initialEntries: [path], future: { v7_startTransition: true, v7_relativeSplatPath: true } },
      createElement(Routes, null,
        createElement(Route, { path: '/dashboard', element: createElement(ProjectDashboardPage) }),
        createElement(Route, { path: '/projects/:projectId/dashboard', element: createElement(ProjectDashboardPage) }),
      )))))
  await flush()
}

it('refreshes both the project and report when the refresh button is clicked', async () => {
  getProject.mockResolvedValue({ name: 'Dự án A' })
  getProjectDashboard.mockResolvedValue({ projectId: 'a', generatedAtUtc: '2026-11-18T00:00:00Z', tasks: { total: 0, todo: 0, inProgress: 0, done: 0, overdue: 0, completionPercent: 0 }, byAssignee: [], activeSprint: null })
  await render('/projects/a/dashboard')
  expect(getProject).toHaveBeenCalledTimes(1)
  expect(getProjectDashboard).toHaveBeenCalledTimes(1)
  await click('Làm mới')
  expect(getProject).toHaveBeenCalledTimes(2)
  expect(getProjectDashboard).toHaveBeenCalledTimes(2)
})

it('navigates project picker pages and links to the selected report', async () => {
  getProjects.mockImplementation(async (page: number) => ({ items: [{ id: String(page), name: `Dự án ${page}` }], page, pageSize: 1, totalCount: 2 }))
  await render('/dashboard')
  expect(container.textContent).toContain('Dự án 1')
  expect(Array.from(container.querySelectorAll('button')).find((button) => button.textContent === 'Trang trước')?.disabled).toBe(true)
  await click('Trang sau')
  expect(getProjects).toHaveBeenCalledWith(2)
  expect(container.textContent).toContain('Dự án 2')
  expect(container.querySelector('a[href="/projects/2/dashboard"]')).not.toBeNull()
  expect(Array.from(container.querySelectorAll('button')).find((button) => button.textContent === 'Trang sau')?.disabled).toBe(true)
  await click('Trang trước')
  expect(container.textContent).toContain('Dự án 1')
})
