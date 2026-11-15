import type { NotificationItem, NotificationsResponse } from '../types/notification.ts'
import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'

export async function getNotifications(): Promise<NotificationsResponse> {
  if (IS_MOCK) {
    return mockService.getNotifications()
  }
  try {
    const data = await apiClient.get<NotificationsResponse | NotificationItem[]>('/api/notifications')
    // Xử lý linh hoạt nếu backend trả về danh sách mảng phẳng hoặc object có unreadCount
    if (Array.isArray(data)) {
      const unreadCount = data.filter((n) => !n.isRead).length
      return { items: data, unreadCount }
    }
    return data
  } catch {
    // Fallback về mock data nếu backend module Notifications chưa được deploy/kết nối
    return mockService.getNotifications()
  }
}

export async function markNotificationAsRead(id: string): Promise<void> {
  if (IS_MOCK) {
    mockService.markNotificationAsRead(id)
    return
  }
  try {
    await apiClient.post(`/api/notifications/${id}/read`)
  } catch {
    mockService.markNotificationAsRead(id)
  }
}

export async function markAllNotificationsAsRead(): Promise<void> {
  if (IS_MOCK) {
    mockService.markAllNotificationsAsRead()
    return
  }
  try {
    await apiClient.post('/api/notifications/read-all')
  } catch {
    mockService.markAllNotificationsAsRead()
  }
}

export function pushMockNotification(item: Omit<NotificationItem, 'id' | 'createdAt' | 'isRead'>): NotificationItem {
  return mockService.addNotification(item)
}

