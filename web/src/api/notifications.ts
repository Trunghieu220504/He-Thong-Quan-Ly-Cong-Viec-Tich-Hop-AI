import type { NotificationDto, NotificationItem, NotificationsResponse, NotificationType } from '../types/notification.ts'
import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'

const notificationTypes: Record<string, NotificationType> = {
  TaskAssigned: 'task_assigned',
  TaskStatusChanged: 'status_changed',
  task_assigned: 'task_assigned',
  status_changed: 'status_changed',
  risk_alert: 'risk_alert',
  system: 'system',
}

export function toNotificationItem(notification: NotificationDto): NotificationItem {
  return { ...notification, type: notificationTypes[notification.type] ?? 'system' }
}

export async function getNotifications(): Promise<NotificationsResponse> {
  if (IS_MOCK) {
    return mockService.getNotifications()
  }

  const [data, unread] = await Promise.all([
    apiClient<{ items: NotificationDto[] } | NotificationDto[]>('/api/notifications'),
    apiClient<{ count: number }>('/api/notifications/unread-count'),
  ])
  const items = Array.isArray(data) ? data : data.items

  return { items: items.map(toNotificationItem), unreadCount: unread.count }
}

export async function markNotificationAsRead(id: string): Promise<void> {
  if (IS_MOCK) {
    mockService.markNotificationAsRead(id)
    return
  }

  await apiClient<void>(`/api/notifications/${id}/read`, { method: 'PATCH' })
}

export async function markAllNotificationsAsRead(): Promise<void> {
  if (IS_MOCK) {
    mockService.markAllNotificationsAsRead()
    return
  }

  await apiClient<void>('/api/notifications/read-all', { method: 'POST' })
}

export function pushMockNotification(item: Omit<NotificationItem, 'id' | 'createdAt' | 'isRead'>): NotificationItem {
  return mockService.addNotification(item)
}

