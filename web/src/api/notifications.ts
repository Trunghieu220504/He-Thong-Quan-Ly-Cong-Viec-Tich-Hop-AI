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
    apiClient<{ items: NotificationDto[]; page: number; pageSize: number; totalCount: number } | NotificationDto[]>('/api/notifications?page=1&pageSize=100'),
    apiClient<{ count: number }>('/api/notifications/unread-count'),
  ])
  const items = [...(Array.isArray(data) ? data : data.items)]
  if (!Array.isArray(data)) {
    const pageSize = data.pageSize || 100
    const pages = Math.ceil(data.totalCount / pageSize)
    for (let page = 2; page <= pages; page++) {
      const next = await apiClient<{ items: NotificationDto[] }>(
        `/api/notifications?page=${page}&pageSize=${pageSize}`,
      )
      items.push(...next.items)
    }
  }

  return { items: [...new Map(items.map((item) => [item.id, item])).values()].map(toNotificationItem), unreadCount: unread.count }
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
