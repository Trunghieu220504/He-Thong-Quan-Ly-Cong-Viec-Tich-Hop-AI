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

  try {
    const query = status ? `?status=${status}` : ''
    return await apiClient<TaskItem[]>(`/api/projects/${projectId}/tasks${query}`)
  } catch (error) {
    console.warn(`Lấy tasks từ backend thất bại (${projectId}), dùng mock fallback:`, error)
    let list = mockService.getTasks(projectId)
    if (status) {
      list = list.filter((t) => t.status === status)
    }
    return list
  }
}

export async function createTask(projectId: string, data: CreateTaskRequest): Promise<TaskItem> {
  if (IS_MOCK) {
    return mockService.createTask(projectId, data)
  }

  try {
    return await apiClient<TaskItem>(`/api/projects/${projectId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(data),
    })
  } catch (error) {
    console.warn('Tạo task trên backend lỗi, lưu tạm vào mock:', error)
    return mockService.createTask(projectId, data)
  }
}

export async function updateTaskStatus(taskId: string, status: TaskStatus): Promise<void> {
  if (IS_MOCK) {
    mockService.updateTaskStatus(taskId, status)
    return
  }

  try {
    await apiClient<void>(`/api/tasks/${taskId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
  } catch (error) {
    console.warn(`Cập nhật status task ${taskId} lỗi, cập nhật trên mock:`, error)
    mockService.updateTaskStatus(taskId, status)
  }
}

export async function updateTaskAssignee(taskId: string, assigneeId?: string): Promise<void> {
  if (IS_MOCK) {
    mockService.updateTaskAssignee(taskId, assigneeId)
    return
  }

  try {
    await apiClient<void>(`/api/tasks/${taskId}/assignee`, {
      method: 'PATCH',
      body: JSON.stringify({ assigneeId }),
    })
  } catch (error) {
    console.warn(`Cập nhật người nhận task ${taskId} lỗi, cập nhật trên mock:`, error)
    mockService.updateTaskAssignee(taskId, assigneeId)
  }
}
