import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { markAllNotificationsAsRead, markNotificationAsRead, pushMockNotification } from '../api/notifications.ts'
import { useNotificationSync } from '../hooks/useNotificationSync.ts'
import { BellIcon } from './AppIcons.tsx'
import type { NotificationItem } from '../types/notification.ts'

function formatTimeAgo(isoString: string): string {
  try {
    const diffMs = Date.now() - new Date(isoString).getTime()
    const diffSec = Math.floor(diffMs / 1000)
    if (diffSec < 60) return 'Vừa xong'
    const diffMin = Math.floor(diffSec / 60)
    if (diffMin < 60) return `${diffMin} phút trước`
    const diffHours = Math.floor(diffMin / 60)
    if (diffHours < 24) return `${diffHours} giờ trước`
    const diffDays = Math.floor(diffHours / 24)
    return `${diffDays} ngày trước`
  } catch {
    return 'Gần đây'
  }
}

function NotificationTypeIcon({ type }: { type: NotificationItem['type'] }) {
  if (type === 'risk_alert') {
    return (
      <span className="notif-type-icon risk" title="Cảnh báo AI">
        🤖
      </span>
    )
  }
  if (type === 'status_changed') {
    return (
      <span className="notif-type-icon status" title="Chuyển trạng thái">
        🔄
      </span>
    )
  }
  if (type === 'task_assigned') {
    return (
      <span className="notif-type-icon assign" title="Được gán công việc">
        📋
      </span>
    )
  }
  return (
    <span className="notif-type-icon info" title="Hệ thống">
      🔔
    </span>
  )
}

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()
  const { notifications, unreadCount } = useNotificationSync()

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  // Mutation đánh dấu một thông báo đã đọc
  const readOneMutation = useMutation({
    mutationFn: (id: string) => markNotificationAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })

  // Mutation đánh dấu tất cả đã đọc
  const readAllMutation = useMutation({
    mutationFn: () => markAllNotificationsAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })

  const handleItemClick = (item: NotificationItem) => {
    if (!item.isRead) {
      readOneMutation.mutate(item.id)
    }
    // Nếu có gắn với task hoặc sự kiện, có thể điều hướng hoặc kích hoạt modal
    if (item.taskId) {
      window.dispatchEvent(new CustomEvent('pmai-open-task-detail', { detail: { taskId: item.taskId } }))
    }
  }

  // Nút giả lập sự kiện thông báo mới để kiểm thử polling / real-time toast
  const handleSimulateNewNotification = () => {
    pushMockNotification({
      title: 'Thông báo Real-time (SignalR / Polling)',
      message: `Đồng nghiệp vừa cập nhật tiến độ lúc ${new Date().toLocaleTimeString('vi-VN')}`,
      type: 'task_assigned',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
    })
    queryClient.invalidateQueries({ queryKey: ['notifications'] })
  }

  return (
    <div className="notification-bell-container" ref={dropdownRef}>
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="header-icon-btn notification-bell-btn"
        title="Thông báo (SignalR + Polling 30 giây)"
        aria-expanded={isOpen}
      >
        <BellIcon />
        {unreadCount > 0 && (
          <span className="notification-badge-count">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="notification-dropdown-panel" role="menu">
          <div className="notification-dropdown-header">
            <div className="header-title-wrap">
              <span className="header-title">Thông báo</span>
              {unreadCount > 0 && (
                <span className="header-unread-pill">{unreadCount} mới</span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => readAllMutation.mutate()}
                className="mark-all-read-btn"
                disabled={readAllMutation.isPending}
              >
                Đánh dấu đã đọc
              </button>
            )}
          </div>

          <div className="notification-dropdown-list custom-scroll">
            {notifications.length === 0 ? (
              <div className="notification-empty-state">
                <span className="empty-bell-icon">🔔</span>
                <p>Không có thông báo nào</p>
              </div>
            ) : (
              notifications.map((item) => (
                <button
                  type="button"
                  key={item.id}
                  onClick={() => handleItemClick(item)}
                  className={`notification-list-item ${!item.isRead ? 'unread' : ''}`}
                >
                  <div className="notif-icon-col">
                    <NotificationTypeIcon type={item.type} />
                  </div>
                  <div className="notif-content-col">
                    <div className="notif-title-row">
                      <span className="notif-item-title">{item.title}</span>
                      {!item.isRead && <span className="notif-unread-dot" />}
                    </div>
                    <p className="notif-item-message">{item.message}</p>
                    <span className="notif-item-time">{formatTimeAgo(item.createdAt)}</span>
                  </div>
                </button>
              ))
            )}
          </div>

          <div className="notification-dropdown-footer">
            <button
              type="button"
              onClick={handleSimulateNewNotification}
              className="test-notify-trigger-btn"
              title="Kích hoạt thông báo mới để kiểm thử Toast popup"
            >
              <span>⚡</span> Thử nhận thông báo mới (Test Toast)
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
