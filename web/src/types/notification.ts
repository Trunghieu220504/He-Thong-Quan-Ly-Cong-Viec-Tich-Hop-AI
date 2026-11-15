export type NotificationType = 'task_assigned' | 'status_changed' | 'risk_alert' | 'system'

export interface NotificationItem {
  id: string
  title: string
  message: string
  type: NotificationType
  isRead: boolean
  createdAt: string
  taskId?: string
  projectId?: string
}

export interface NotificationsResponse {
  items: NotificationItem[]
  unreadCount: number
}

