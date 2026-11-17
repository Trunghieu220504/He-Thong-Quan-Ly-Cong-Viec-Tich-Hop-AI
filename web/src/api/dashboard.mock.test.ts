import { afterEach, beforeEach, expect, it, vi } from 'vitest'
vi.mock('./client.ts', () => ({ IS_MOCK: true, apiClient: vi.fn() }))
import { getProjectDashboard } from './dashboard.ts'

const storage = new Map<string, string>()
beforeEach(() => {
  storage.clear()
  vi.stubGlobal('localStorage', { getItem: (key: string) => storage.get(key) ?? null, setItem: (key: string, value: string) => storage.set(key, value) })
  vi.useFakeTimers()
  vi.setSystemTime(new Date('2026-11-18T00:00:00Z'))
})
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers() })

it('aggregates every mock task in the selected project and excludes completed tasks from overdue', async () => {
  storage.set('pmai_mock_db_v4', JSON.stringify({
    projects: [{ id: 'project-1', members: [{ userId: 'member-1', displayName: 'An' }] }],
    tasks: [
      ...Array.from({ length: 111 }, (_, index) => ({ id: String(index), projectId: 'project-1', status: 'Todo', dueDate: '2026-11-17T00:00:00Z' })),
      { id: 'done', projectId: 'project-1', status: 'Done', assigneeId: 'member-1', dueDate: '2026-11-17T00:00:00Z' },
      { id: 'future', projectId: 'project-1', status: 'InProgress', assigneeId: 'member-1', dueDate: '2026-11-19T00:00:00Z' },
      { id: 'other', projectId: 'project-2', status: 'Done' },
    ],
  }))
  const result = await getProjectDashboard('project-1')
  expect(result.tasks).toEqual({ total: 113, todo: 111, inProgress: 1, done: 1, overdue: 111, completionPercent: 0.88 })
  expect(result.byAssignee).toEqual([
    { assigneeId: null, displayName: 'Chưa giao', total: 111, done: 0 },
    { assigneeId: 'member-1', displayName: 'An', total: 2, done: 1 },
  ])
  expect(result.activeSprint).toBeNull()
})

it('returns empty statistics for an empty project', async () => {
  storage.set('pmai_mock_db_v4', JSON.stringify({ projects: [{ id: 'empty', members: [] }], tasks: [] }))
  const result = await getProjectDashboard('empty')
  expect(result.tasks.total).toBe(0)
  expect(result.tasks.completionPercent).toBe(0)
  expect(result.byAssignee).toEqual([])
  expect(result.activeSprint).toBeNull()
})

it('rejects a missing project instead of leaking another project report', async () => {
  storage.set('pmai_mock_db_v4', JSON.stringify({ projects: [], tasks: [] }))
  await expect(getProjectDashboard('missing')).rejects.toThrow('Không tìm thấy dự án')
})
