import { useEffect, useRef } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { API_BASE_URL, IS_MOCK } from '../api/client.ts'
import { getNotifications, toNotificationItem } from '../api/notifications.ts'
import { useAuthStore } from '../stores/authStore.ts'
import { toast } from '../stores/toastStore.ts'
import type { NotificationDto, NotificationItem } from '../types/notification.ts'

// Cấu hình URL SignalR Hub cho Notifications
const HUB_URL = import.meta.env.VITE_NOTIFICATION_HUB_URL || `${API_BASE_URL.replace(/\/$/, '')}/hubs/notifications`

export function useNotificationSync() {
  const queryClient = useQueryClient()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const seenNotificationIdsRef = useRef<Set<string>>(new Set())
  const isInitialLoadRef = useRef(true)

  // Polling 30 giây theo đúng yêu cầu Jira PMS-159 (TanStack Query refetchInterval)
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['notifications'],
    queryFn: getNotifications,
    enabled: isAuthenticated,
    refetchInterval: 30000, // 30 giây tự động poll một lần
    refetchIntervalInBackground: true,
  })

  // Phát hiện thông báo mới và kích hoạt Toast Notification
  useEffect(() => {
    if (!data?.items) return

    const currentItems = data.items
    const seenIds = seenNotificationIdsRef.current

    if (isInitialLoadRef.current) {
      // Lần đầu tải: đánh dấu các thông báo hiện có để không bắn toast đồng loạt
      currentItems.forEach((item) => seenIds.add(item.id))
      isInitialLoadRef.current = false
      return
    }

    // Tìm các thông báo mới chưa từng thấy
    const newItems = currentItems.filter((item) => !seenIds.has(item.id) && !item.isRead)

    newItems.forEach((item) => {
      seenIds.add(item.id)
      triggerToastForNotification(item)
    })
  }, [data])

  // Thiết lập SignalR với accessTokenFactory lấy token mới mỗi lần kết nối
  useEffect(() => {
    if (IS_MOCK || !isAuthenticated || !useAuthStore.getState().tokens?.accessToken) return

    let isDisposed = false

    // Định nghĩa hàm accessTokenFactory theo đúng yêu cầu kỹ thuật PMS-159
    const accessTokenFactory = () => {
      const currentTokens = useAuthStore.getState().tokens
      return currentTokens?.accessToken || ''
    }

    // Polling tiếp tục hoạt động khi SignalR chưa kết nối được.
    async function initSignalR() {
      try {
        const signalR = await import('@microsoft/signalr')
        if (isDisposed) return

        const connection = new signalR.HubConnectionBuilder()
          .withUrl(HUB_URL, {
            accessTokenFactory,
            withCredentials: false,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
          .build()

        connection.on('notificationReceived', (notification: NotificationDto) => {
          if (isDisposed) return
          const notif = toNotificationItem(notification)
          seenNotificationIdsRef.current.add(notif.id)
          triggerToastForNotification(notif)
          queryClient.invalidateQueries({ queryKey: ['notifications'] })
        })

        await connection.start()

        return () => {
          isDisposed = true
          connection.stop().catch(() => {})
        }
      } catch {
        // Nếu SignalR chưa chạy ở backend hoặc chưa cài gói, hệ thống tự động chạy chế độ polling 30 giây
      }
    }

    const cleanupPromise = initSignalR()

    return () => {
      isDisposed = true
      cleanupPromise.then((cleanup) => cleanup && cleanup())
    }
  }, [isAuthenticated, queryClient])

  return {
    notifications: data?.items || [],
    unreadCount: data?.unreadCount ?? (data?.items ? data.items.filter((i) => !i.isRead).length : 0),
    isLoading,
    refetch,
  }
}

function triggerToastForNotification(item: NotificationItem) {
  if (item.type === 'risk_alert') {
    toast.warning(item.title, item.message)
  } else if (item.type === 'status_changed') {
    toast.info(item.title, item.message)
  } else if (item.type === 'task_assigned') {
    toast.success(item.title, item.message)
  } else {
    toast.info(item.title, item.message)
  }
}
