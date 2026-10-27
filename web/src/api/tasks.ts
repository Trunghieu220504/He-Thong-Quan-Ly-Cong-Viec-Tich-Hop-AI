import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { CreateTaskRequest, TaskItem, TaskStatus } from '../types/task.ts'

export async function getTasks(projectId: string, status?: TaskStatus): Promise<TaskItem[]> {
  if (IS_MOCK) {
    let list = mockService.getTasks(projectId)
    if (status) {
      list = list.filter((t) => t.status === status)
    }
    return list
  }

  const query = status ? `?status=${status}` : ''
  return apiClient<TaskItem[]>(`/api/projects/${projectId}/tasks${query}`)
}

export async function createTask(projectId: string, data: CreateTaskRequest): Promise<TaskItem> {
  if (IS_MOCK) {
    return mockService.createTask(projectId, data)
  }

  return apiClient<TaskItem>(`/api/projects/${projectId}/tasks`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateTaskStatus(taskId: string, status: TaskStatus): Promise<void> {
  if (IS_MOCK) {
    mockService.updateTaskStatus(taskId, status)
    return
  }

  await apiClient<void>(`/api/tasks/${taskId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}

export async function updateTaskAssignee(taskId: string, assigneeId?: string): Promise<void> {
  if (IS_MOCK) {
    mockService.updateTaskAssignee(taskId, assigneeId)
    return
  }

  await apiClient<void>(`/api/tasks/${taskId}/assignee`, {
    method: 'PATCH',
    body: JSON.stringify({ assigneeId }),
  })
}
