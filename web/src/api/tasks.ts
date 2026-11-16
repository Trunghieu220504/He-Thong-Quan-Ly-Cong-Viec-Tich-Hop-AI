import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { CreateTaskRequest, TaskItem, TaskStatus } from '../types/task.ts'

interface PagedTasksResponse {
  items: TaskItem[]
  page: number
  pageSize: number
  totalCount: number
}

/**
 * PMS-157: [W0] getTasks đọc dạng phân trang, mặc định pageSize=100
 */
export async function getTasks(
  projectId: string,
  page = 1,
  pageSize = 100,
  status?: TaskStatus,
): Promise<TaskItem[]> {
  if (IS_MOCK) {
    return mockService.getTasks(projectId, page, pageSize, status)
  }

  const queryParams = new URLSearchParams()
  queryParams.set('page', String(page))
  queryParams.set('pageSize', String(pageSize))
  if (status) {
    queryParams.set('status', status)
  }

  const response = await apiClient<PagedTasksResponse | TaskItem[]>(
    `/api/projects/${projectId}/tasks?${queryParams.toString()}`,
  )

  // Bóc tách linh hoạt: nếu backend trả PagedResult { items: [...] } thì lấy items, nếu trả mảng thì lấy trực tiếp
  if (response && 'items' in response && Array.isArray(response.items)) {
    return response.items
  }
  if (Array.isArray(response)) {
    return response
  }
  return []
}

export async function getTaskById(taskId: string): Promise<TaskItem> {
  if (IS_MOCK) {
    const task = mockService.getTasks('').find((t) => t.id === taskId)
    if (task) return task
  }

  return apiClient<TaskItem>(`/api/tasks/${taskId}`)
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
    body: JSON.stringify({ assigneeId: assigneeId ?? null }),
  })
}

/**
 * PMS-158: Cập nhật chi tiết task (kết hợp status, assignee và các trường mở rộng)
 */
export async function updateTask(taskId: string, data: Partial<TaskItem>): Promise<void> {
  if (IS_MOCK) {
    mockService.updateTask(taskId, data)
    return
  }

  // PUT replaces all details; preserve fields that the modal does not edit.
  const current = await getTaskById(taskId)
  const progressPercent = data.progressPercent ?? current.progressPercent
  if (progressPercent === undefined || progressPercent === null) {
    throw new Error('Không thể lưu: thiếu dữ liệu tiến độ hiện tại.')
  }
  await apiClient<void>(`/api/tasks/${taskId}`, {
    method: 'PUT',
    body: JSON.stringify({
      title: data.title ?? current.title,
      description: 'description' in data ? data.description ?? null : current.description ?? null,
      priority: data.priority ?? current.priority,
      dueDate: 'dueDate' in data ? data.dueDate ?? null : current.dueDate ?? null,
      estimatedHours: data.estimatedHours === undefined ? current.estimatedHours ?? null : data.estimatedHours,
      progressPercent,
    }),
  })

  // Cập nhật status nếu có
  if (data.status !== undefined && data.status !== current.status) {
    await updateTaskStatus(taskId, data.status)
  }

  // Cập nhật assignee nếu có
  if ('assigneeId' in data && (data.assigneeId ?? null) !== (current.assigneeId ?? null)) {
    await updateTaskAssignee(taskId, data.assigneeId)
  }
}
