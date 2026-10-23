export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export type TaskPriority = 'Low' | 'Medium' | 'High'

export type TaskRiskLevel = 'low' | 'medium' | 'high'

export interface TaskItem {
  id: string
  projectId: string
  title: string
  description?: string
  status: TaskStatus
  priority: TaskPriority
  assigneeId?: string
  assigneeName?: string
  dueDate?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateTaskRequest {
  title: string
  description?: string
  priority: TaskPriority
  assigneeId?: string
  dueDate?: string
}

export interface UpdateTaskStatusRequest {
  status: TaskStatus
}

export interface UpdateTaskAssigneeRequest {
  assigneeId?: string
}

export interface TaskRiskResponse {
  taskId: string
  risk: TaskRiskLevel
  source: string
}
