import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { API_BASE_URL } from './client.ts'
import { getNotifications, markAllNotificationsAsRead, markNotificationAsRead } from './notifications.ts'
import { useAuthStore } from '../stores/authStore.ts'

const notification = {
  id: '84e97b5a-b15b-4cb4-91ee-06852ff18f97',
  title: 'Bạn được giao công việc mới',
  message: 'Một công việc đã được giao cho bạn.',
  type: 'TaskAssigned',
  isRead: false,
  createdAt: '2026-11-16T00:00:00Z',
}

describe('Notifications API contract', () => {
  beforeEach(() => {
    useAuthStore.setState({ tokens: null, user: null, isAuthenticated: false })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('reads paged notifications and the total unread count from separate endpoints', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({
        items: [notification, { ...notification, id: 'status-notification', type: 'TaskStatusChanged' }],
        page: 1,
        pageSize: 20,
        totalCount: 40,
      }))
      .mockResolvedValueOnce(Response.json({ count: 25 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getNotifications()).resolves.toEqual({
      items: [
        { ...notification, type: 'task_assigned' },
        { ...notification, id: 'status-notification', type: 'status_changed' },
      ],
      unreadCount: 25,
    })
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
      `${API_BASE_URL}/api/notifications`,
      `${API_BASE_URL}/api/notifications/unread-count`,
    ])
  })

  it('supports array responses containing existing frontend notification types', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json([{ ...notification, type: 'risk_alert' }]))
      .mockResolvedValueOnce(Response.json({ count: 1 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(getNotifications()).resolves.toEqual({
      items: [{ ...notification, type: 'risk_alert' }],
      unreadCount: 1,
    })
  })

  it('uses PATCH to read one notification and POST to read all', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ ...notification, isRead: true }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await markNotificationAsRead(notification.id)
    await markAllNotificationsAsRead()

    expect(fetchMock).toHaveBeenNthCalledWith(1,
      `${API_BASE_URL}/api/notifications/${notification.id}/read`,
      expect.objectContaining({ method: 'PATCH' }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(2,
      `${API_BASE_URL}/api/notifications/read-all`,
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it.each([403, 503])('propagates HTTP %s errors to the caller', async (status) => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(Response.json({ detail: 'Notifications unavailable' }, { status }))
      .mockResolvedValueOnce(Response.json({ count: 0 })))

    await expect(getNotifications()).rejects.toMatchObject({
      status,
      message: 'Notifications unavailable',
    })
  })

  it('reports a failed read instead of treating it as a successful local change', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ detail: 'Notification not found' }, { status: 404 })))

    await expect(markNotificationAsRead(notification.id)).rejects.toMatchObject({ status: 404 })
  })
})
