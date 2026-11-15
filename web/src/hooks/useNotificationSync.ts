import { useEffect, useRef } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getNotifications } from '../api/notifications.ts'
import { useAuthStore } from '../stores/authStore.ts'
import { toast } from '../stores/toastStore.ts'
import type { NotificationItem } from '../types/notification.ts'

// Cấu hình URL SignalR Hub cho Notifications
const HUB_URL = import.meta.env.VITE_NOTIFICATION_HUB_URL || 'http://localhost:5046/hubs/notifications'

export function useNotificationSync() {
  const queryClient = useQueryClient()
  const { isAuthenticated, tokens } = useAuthStore()
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
    if (!isAuthenticated || !tokens?.accessToken) return

    let isDisposed = false

    // Định nghĩa hàm accessTokenFactory theo đúng yêu cầu kỹ thuật PMS-159
    const accessTokenFactory = () => {
      const currentTokens = useAuthStore.getState().tokens
      return currentTokens?.accessToken || ''
    }

    // Thử kết nối SignalR qua dynamic import hoặc WebSocket fallback nếu backend sẵn sàng
    async function initSignalR() {
      try {
        // Kiểm tra xem @microsoft/signalr có khả dụng không
        // @ts-expect-error dynamic import check
        const signalR = await import('@microsoft/signalr').catch(() => null)
        if (!signalR || isDisposed) return

        const connection = new signalR.HubConnectionBuilder()
          .withUrl(HUB_URL, {
            accessTokenFactory: () => accessTokenFactory(),
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
          .build()

        connection.on('ReceiveNotification', (notif: NotificationItem) => {
          if (isDisposed) return
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
  }, [isAuthenticated, tokens?.accessToken, queryClient])

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

