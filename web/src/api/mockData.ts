import { ApiError } from './client.ts'
import type { AuthTokens, UserSummary } from '../types/auth.ts'
import type { NotificationItem, NotificationsResponse } from '../types/notification.ts'
import type { ProjectDetail, ProjectsListResponse } from '../types/project.ts'
import type { TaskItem, TaskRiskResponse } from '../types/task.ts'
import type { ProjectDashboard } from '../types/dashboard.ts'

const STORAGE_KEY = 'pmai_mock_db_v4'

interface MockDB {
  projects: ProjectDetail[]
  tasks: TaskItem[]
  risks: Record<string, TaskRiskResponse>
  notifications: NotificationItem[]
}

function initMockData(): MockDB {
  const initialProjects: ProjectDetail[] = [
    {
      id: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      name: 'Hệ Thống Quản Lý Công Việc Tích Hợp AI',
      description: 'Dự án môn học: Modular Monolith .NET 10, React Vite Frontend, FastAPI AI Service.',
      ownerId: 'u-user-01',
      myRole: 'Owner',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      members: [
        { userId: 'u-user-01', role: 'Owner', joinedAt: new Date().toISOString(), displayName: 'Bảo Anh (Bạn)' },
        { userId: 'u-user-02', role: 'Admin', joinedAt: new Date().toISOString(), displayName: 'Trung Hiếu' },
        { userId: 'u-user-03', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Hoàng Xuân' },
        { userId: 'u-user-04', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Phạm Đăng' },
        { userId: 'u-user-05', role: 'Viewer', joinedAt: new Date().toISOString(), displayName: 'Phương Hoa' },
      ],
    },
    {
      id: 'f2d3c4b5-b6f7-4890-9123-4567890bcdef',
      name: 'HealthBook AI',
      description: 'Hệ thống hồ sơ sức khỏe và gợi ý chẩn đoán y khoa AI.',
      ownerId: 'u-user-02',
      myRole: 'Member',
      createdAt: new Date(Date.now() - 86400000 * 2).toISOString(),
      members: [
        { userId: 'u-user-02', role: 'Owner', joinedAt: new Date().toISOString(), displayName: 'Trung Hiếu' },
        { userId: 'u-user-01', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Bảo Anh (Bạn)' },
      ],
    },
    {
      id: 'a3b4c5d6-e7f8-4901-a234-5678901cdef0',
      name: 'Hệ thống quản lý mượn trả thiết bị',
      description: 'Quản lý thiết bị phần cứng, phòng máy và cảm biến IoT.',
      ownerId: 'u-user-03',
      myRole: 'Member',
      createdAt: new Date(Date.now() - 86400000 * 4).toISOString(),
      members: [
        { userId: 'u-user-03', role: 'Owner', joinedAt: new Date().toISOString(), displayName: 'Hoàng Xuân' },
        { userId: 'u-user-01', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Bảo Anh (Bạn)' },
      ],
    },
  ]

  const initialTasks: TaskItem[] = [
    {
      id: 'task-157',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: '[W0] getTasks đọc dạng phân trang, pageSize=100',
      description: 'Cập nhật apiClient và getTasks gọi /api/projects/{projectId}/tasks?page=1&pageSize=100, bóc tách cấu trúc PagedResult.',
      status: 'Todo',
      priority: 'Medium',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-158',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: '[W1] Modal chi tiết/sửa task + mức rủi ro',
      description: 'Modal chi tiết công việc cho phép xem và sửa tiêu đề, mô tả, mức ưu tiên, người phụ trách, trạng thái và hiển thị đánh giá rủi ro AI.',
      status: 'Todo',
      priority: 'High',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000 * 2).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-159',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: '[W2] Chuông thông báo (SignalR + polling 30 giây) + toast',
      description: 'Ước tính 3. accessTokenFactory lấy token mới mỗi lần nối. Kết nối SignalR hub hoặc fallback polling 30 giây để cập nhật thông báo và hiển thị toast.',
      status: 'Todo',
      priority: 'High',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000 * 3).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-152',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Dashboard backend + đặc trưng RiskAI',
      description: 'Xây dựng API thống kê tổng quan và trích xuất vector đặc trưng cho mô hình dự đoán trễ hạn.',
      status: 'InProgress',
      priority: 'High',
      assigneeId: 'u-user-04',
      assigneeName: 'Phạm Đăng',
      dueDate: new Date(Date.now() + 86400000 * 2).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-156',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Web: thông báo, dashboard, sửa task',
      description: 'Hoàn thiện giao diện thông báo real-time SignalR và modal cập nhật chi tiết công việc.',
      status: 'InProgress',
      priority: 'High',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000 * 3).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-163',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Docker hóa API + Web',
      description: 'Viết Dockerfile nhiều tầng (multi-stage build) và docker-compose.yml cho môi trường dev/staging.',
      status: 'InReview' as TaskItem['status'],
      priority: 'High',
      assigneeId: 'u-user-03',
      assigneeName: 'Hoàng Xuân',
      dueDate: new Date(Date.now() + 86400000).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-149',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'ML thật cho ai-service',
      description: 'Huấn luyện mô hình Random Forest / Logistic Regression dự đoán mức độ trễ của Task.',
      status: 'Done',
      priority: 'High',
      assigneeId: 'u-user-04',
      assigneeName: 'Phạm Đăng',
      dueDate: new Date().toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-168',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Tích hợp RabbitMQ EventBus cho Module Notifications',
      description: 'Lắng nghe sự kiện TaskCreatedEvent và gửi email thông báo cho Assignee.',
      status: 'InProgress',
      priority: 'Medium',
      assigneeId: 'u-user-02',
      assigneeName: 'Trung Hiếu',
      dueDate: new Date(Date.now() + 86400000 * 4).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-170',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Kiểm thử Smoke E2E cho luồng phân tích rủi ro',
      description: 'Kiểm tra trạng thái khi AI Service hoạt động và khi AI Service bị ngắt kết nối.',
      status: 'InProgress',
      priority: 'Medium',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000 * 5).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
  ]

  const initialRisks: Record<string, TaskRiskResponse> = {
    'task-157': { taskId: 'task-157', risk: 'low', source: 'ai-service' },
    'task-158': { taskId: 'task-158', risk: 'medium', source: 'ai-service' },
    'task-159': { taskId: 'task-159', risk: 'medium', source: 'ai-service' },
    'task-152': { taskId: 'task-152', risk: 'low', source: 'ai-service' },
    'task-156': { taskId: 'task-156', risk: 'medium', source: 'ai-service' },
    'task-163': { taskId: 'task-163', risk: 'low', source: 'ai-service' },
    'task-149': { taskId: 'task-149', risk: 'low', source: 'ai-service' },
    'task-168': { taskId: 'task-168', risk: 'high', source: 'ai-service' },
    'task-170': { taskId: 'task-170', risk: 'medium', source: 'ai-service' },
  }

  const initialNotifications: NotificationItem[] = [
    {
      id: 'notif-1',
      title: 'Công việc mới được giao',
      message: 'Bạn được gán vào subtask PMS-159: [W2] Chuông thông báo (SignalR + polling 30 giây) + toast',
      type: 'task_assigned',
      isRead: false,
      createdAt: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
      taskId: 'task-159',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
    },
    {
      id: 'notif-2',
      title: 'Cảnh báo rủi ro AI',
      message: 'Mô hình AI dự đoán rủi ro cao ở task PMS-168: Tích hợp RabbitMQ EventBus',
      type: 'risk_alert',
      isRead: false,
      createdAt: new Date(Date.now() - 1000 * 60 * 25).toISOString(),
      taskId: 'task-168',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
    },
    {
      id: 'notif-3',
      title: 'Cập nhật trạng thái công việc',
      message: 'Hoàng Xuân đã chuyển task PMS-163 (Docker hóa API + Web) sang In Review',
      type: 'status_changed',
      isRead: false,
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(),
      taskId: 'task-163',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
    },
    {
      id: 'notif-4',
      title: 'Thành viên mới',
      message: 'Phương Hoa đã tham gia dự án Hệ Thống Quản Lý Công Việc Tích Hợp AI với vai trò Viewer',
      type: 'system',
      isRead: true,
      createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
    },
  ]

  return {
    projects: initialProjects,
    tasks: initialTasks,
    risks: initialRisks,
    notifications: initialNotifications,
  }
}

function loadDB(): MockDB {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    const data = initMockData()
    saveDB(data)
    return data
  }
  try {
    return JSON.parse(raw) as MockDB
  } catch {
    const data = initMockData()
    saveDB(data)
    return data
  }
}

function saveDB(db: MockDB) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(db))
}

export const mockService = {
  getProjectDashboard(projectId: string): ProjectDashboard {
    const db = loadDB()
    const project = db.projects.find((p) => p.id === projectId)
    if (!project) throw new ApiError(404, 'Không tìm thấy dự án')
    const tasks = db.tasks.filter((task) => task.projectId === projectId)
    const now = new Date()
    const done = tasks.filter((task) => task.status === 'Done').length
    const groups = new Map<string | null, ProjectDashboard['byAssignee'][number]>()
    for (const task of tasks) {
      const id = task.assigneeId || null
      const group = groups.get(id) ?? {
        assigneeId: id,
        displayName: id
          ? project.members.find((member) => member.userId === id)?.displayName || task.assigneeName || id
          : 'Chưa giao',
        total: 0,
        done: 0,
      }
      group.total++
      if (task.status === 'Done') group.done++
      groups.set(id, group)
    }
    return {
      projectId,
      generatedAtUtc: now.toISOString(),
      tasks: {
        total: tasks.length,
        todo: tasks.filter((task) => task.status === 'Todo').length,
        inProgress: tasks.filter((task) => task.status === 'InProgress').length,
        done,
        overdue: tasks.filter((task) => task.status !== 'Done' && task.dueDate && new Date(task.dueDate) < now).length,
        completionPercent: tasks.length ? Math.round(10000 * done / tasks.length) / 100 : 0,
      },
      byAssignee: [...groups.values()],
      // MockDB has no sprint lifecycle; do not invent an active sprint.
      activeSprint: null,
    }
  },

  login(email: string): { tokens: AuthTokens; user: UserSummary } {
    return {
      tokens: {
        accessToken: `mock-jwt-token-${Date.now()}`,
        accessTokenExpiresAtUtc: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
        refreshToken: `mock-refresh-token-${Date.now()}`,
        refreshTokenExpiresAtUtc: new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString(),
      },
      user: {
        id: 'u-user-01',
        email,
        displayName: email.split('@')[0] || 'Bảo Anh',
      },
    }
  },

  getProjects(): ProjectsListResponse {
    const db = loadDB()
    const items = db.projects.map((p) => ({
      id: p.id,
      name: p.name,
      description: p.description,
      ownerId: p.ownerId,
      myRole: p.myRole,
      memberCount: p.members.length,
      createdAt: p.createdAt,
    }))
    return {
      items,
      page: 1,
      pageSize: 20,
      totalCount: items.length,
    }
  },

  getProject(id: string): ProjectDetail {
    const db = loadDB()
    const project = db.projects.find((p) => p.id === id)
    if (!project) throw new ApiError(404, 'Không tìm thấy dự án')
    return project
  },

  createProject(name: string, description?: string): { id: string } {
    const db = loadDB()
    const newId = `proj-${Date.now()}`
    const newProject: ProjectDetail = {
      id: newId,
      name,
      description,
      ownerId: 'u-user-01',
      myRole: 'Owner',
      createdAt: new Date().toISOString(),
      members: [
        { userId: 'u-user-01', role: 'Owner', joinedAt: new Date().toISOString(), displayName: 'Bảo Anh (Bạn)' },
      ],
    }
    db.projects.unshift(newProject)
    saveDB(db)
    return { id: newId }
  },

  getTasks(projectId: string, page = 1, pageSize = 100, status?: TaskItem['status']): TaskItem[] {
    const db = loadDB()
    let list = db.tasks.filter((t) => t.projectId === projectId)
    if (status) {
      list = list.filter((t) => t.status === status)
    }
    const start = (page - 1) * pageSize
    return list.slice(start, start + pageSize)
  },

  updateTask(taskId: string, data: Partial<TaskItem>): TaskItem {
    const db = loadDB()
    const task = db.tasks.find((t) => t.id === taskId)
    if (!task) throw new Error('Không tìm thấy công việc')

    if (data.title !== undefined) task.title = data.title
    if (data.description !== undefined) task.description = data.description
    if (data.priority !== undefined) task.priority = data.priority
    if (data.status !== undefined) task.status = data.status
    if (data.dueDate !== undefined) task.dueDate = data.dueDate

    if (data.assigneeId !== undefined) {
      task.assigneeId = data.assigneeId
      const project = db.projects.find((p) => p.id === task.projectId)
      const m = project?.members.find((mem) => mem.userId === data.assigneeId)
      task.assigneeName = m?.displayName || data.assigneeId
    }

    task.updatedAt = new Date().toISOString()
    saveDB(db)
    return task
  },

  createTask(projectId: string, data: { title: string; description?: string; priority: TaskItem['priority']; assigneeId?: string; dueDate?: string }): TaskItem {
    const db = loadDB()
    const project = db.projects.find((p) => p.id === projectId)
    let assigneeName: string | undefined
    if (data.assigneeId && project) {
      const m = project.members.find((member) => member.userId === data.assigneeId)
      assigneeName = m?.displayName || m?.userId
    }

    const newTask: TaskItem = {
      id: `task-${Date.now()}`,
      projectId,
      title: data.title,
      description: data.description,
      priority: data.priority,
      status: 'Todo',
      assigneeId: data.assigneeId,
      assigneeName,
      dueDate: data.dueDate,
      createdAt: new Date().toISOString(),
    }

    db.tasks.push(newTask)
    // Tự sinh mock risk
    const risks: ('low' | 'medium' | 'high')[] = ['low', 'medium', 'high']
    const assignedRisk = risks[Math.floor(Math.random() * risks.length)]
    db.risks[newTask.id] = {
      taskId: newTask.id,
      risk: assignedRisk,
      source: 'ai-service',
    }

    saveDB(db)
    return newTask
  },

  updateTaskStatus(taskId: string, status: TaskItem['status']): void {
    const db = loadDB()
    const task = db.tasks.find((t) => t.id === taskId)
    if (task) {
      task.status = status
      task.updatedAt = new Date().toISOString()
      saveDB(db)
    }
  },

  updateTaskAssignee(taskId: string, assigneeId?: string): void {
    const db = loadDB()
    const task = db.tasks.find((t) => t.id === taskId)
    if (task) {
      task.assigneeId = assigneeId
      const project = db.projects.find((p) => p.id === task.projectId)
      const m = project?.members.find((mem) => mem.userId === assigneeId)
      task.assigneeName = m?.displayName || assigneeId
      task.updatedAt = new Date().toISOString()
      saveDB(db)
    }
  },

  addProjectMember(projectId: string, data: { userId: string; role: ProjectDetail['myRole']; displayName?: string }): void {
    const db = loadDB()
    const project = db.projects.find((p) => p.id === projectId)
    if (project) {
      const existing = project.members.find((m) => m.userId === data.userId)
      if (!existing) {
        project.members.push({
          userId: data.userId,
          role: data.role,
          joinedAt: new Date().toISOString(),
          displayName: data.displayName || data.userId,
        })
        saveDB(db)
      }
    }
  },

  getTaskRisk(taskId: string): TaskRiskResponse | null {
    const db = loadDB()
    return db.risks[taskId] || null
  },

  getNotifications(): NotificationsResponse {
    const db = loadDB()
    const items = db.notifications || []
    const unreadCount = items.filter((n) => !n.isRead).length
    return { items, unreadCount }
  },

  markNotificationAsRead(id: string): void {
    const db = loadDB()
    if (db.notifications) {
      const notif = db.notifications.find((n) => n.id === id)
      if (notif) {
        notif.isRead = true
        saveDB(db)
      }
    }
  },

  markAllNotificationsAsRead(): void {
    const db = loadDB()
    if (db.notifications) {
      db.notifications.forEach((n) => {
        n.isRead = true
      })
      saveDB(db)
    }
  },

  addNotification(item: Omit<NotificationItem, 'id' | 'createdAt' | 'isRead'>): NotificationItem {
    const db = loadDB()
    if (!db.notifications) db.notifications = []
    const newNotif: NotificationItem = {
      ...item,
      id: 'notif-' + Date.now(),
      createdAt: new Date().toISOString(),
      isRead: false,
    }
    db.notifications.unshift(newNotif)
    saveDB(db)
    return newNotif
  },
}
