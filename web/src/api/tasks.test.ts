import { beforeEach, expect, it, vi } from 'vitest'
vi.mock('./client.ts', () => ({ IS_MOCK: false, apiClient: vi.fn() }))
import { apiClient } from './client.ts'
import { updateTask } from './tasks.ts'
const client = vi.mocked(apiClient)
beforeEach(() => {
  client.mockReset()
  client.mockResolvedValueOnce({ title: 'Old', priority: 'Low', status: 'Todo', assigneeId: 'user-1', estimatedHours: 12, progressPercent: 45 })
  client.mockResolvedValue(undefined)
})
it('saves details, preserves hidden fields and explicitly unassigns', async () => {
  await updateTask('id', { title: 'New', description: undefined, priority: 'High', dueDate: undefined, status: 'Todo', assigneeId: undefined })
  expect(client).toHaveBeenNthCalledWith(2, '/api/tasks/id', {
    method: 'PUT', body: JSON.stringify({ title: 'New', description: null, priority: 'High', dueDate: null, estimatedHours: 12, progressPercent: 45 }),
  })
  expect(client).toHaveBeenLastCalledWith('/api/tasks/id/assignee', { method: 'PATCH', body: '{"assigneeId":null}' })
})
it('rejects failed detail saves without sending subsequent patches', async () => {
  client.mockRejectedValueOnce(new Error('Save failed'))
  await expect(updateTask('id', { title: 'New', status: 'Done' })).rejects.toThrow('Save failed')
  expect(client).toHaveBeenCalledTimes(2)
})

it('does not patch unchanged status or assignee', async () => {
  await updateTask('id', { status: 'Todo', assigneeId: 'user-1' })
  expect(client).toHaveBeenCalledTimes(2)
})
it('patches changed status and assignee', async () => {
  await updateTask('id', { status: 'InProgress', assigneeId: 'user-2' })
  expect(client).toHaveBeenNthCalledWith(3, '/api/tasks/id/status', { method: 'PATCH', body: '{"status":"InProgress"}' })
  expect(client).toHaveBeenNthCalledWith(4, '/api/tasks/id/assignee', { method: 'PATCH', body: '{"assigneeId":"user-2"}' })
})
it('preserves explicit zero progress and clears estimated hours', async () => {
  await updateTask('id', { progressPercent: 0, estimatedHours: null })
  const body = JSON.parse(client.mock.calls[1][1]!.body as string)
  expect(body.progressPercent).toBe(0)
  expect(body.estimatedHours).toBeNull()
  expect(Object.keys(body)).toHaveLength(6)
})
it('refuses to PUT when progress is unavailable', async () => {
  client.mockReset()
  client.mockResolvedValueOnce({ title: 'Old', priority: 'Low' })
  await expect(updateTask('id', { title: 'New' })).rejects.toThrow('thiếu dữ liệu tiến độ')
  expect(client).toHaveBeenCalledTimes(1)
})
it('does not patch an already unassigned task', async () => {
  client.mockReset()
  client.mockResolvedValueOnce({ title: 'Old', priority: 'Low', progressPercent: 30, assigneeId: null })
  await updateTask('id', { assigneeId: undefined })
  expect(client).toHaveBeenCalledTimes(2)
})
