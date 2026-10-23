import type { AuthTokens, UserSummary } from '../types/auth.ts'
import type { ProjectDetail, ProjectsListResponse } from '../types/project.ts'
import type { TaskItem, TaskRiskResponse } from '../types/task.ts'

const STORAGE_KEY = 'pmai_mock_db_v1'

interface MockDB {
  projects: ProjectDetail[]
  tasks: TaskItem[]
  risks: Record<string, TaskRiskResponse>
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
        { userId: 'u-user-03', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Hải Đăng' },
        { userId: 'u-user-04', role: 'Viewer', joinedAt: new Date().toISOString(), displayName: 'Phương Thảo' },
      ],
    },
    {
      id: 'f2d3c4b5-b6f7-4890-9123-4567890bcdef',
      name: 'Website Thương Mại Điện Tử',
      description: 'Nghiên cứu giao diện giỏ hàng và thanh toán trực tuyến.',
      ownerId: 'u-user-02',
      myRole: 'Member',
      createdAt: new Date(Date.now() - 86400000 * 2).toISOString(),
      members: [
        { userId: 'u-user-02', role: 'Owner', joinedAt: new Date().toISOString(), displayName: 'Trung Hiếu' },
        { userId: 'u-user-01', role: 'Member', joinedAt: new Date().toISOString(), displayName: 'Bảo Anh (Bạn)' },
      ],
    },
  ]

  const initialTasks: TaskItem[] = [
    {
      id: 'task-101',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Thiết kế khung giao diện Kanban 3 cột',
      description: 'Tích hợp @dnd-kit/core cho các cột Todo, InProgress, Done.',
      status: 'InProgress',
      priority: 'High',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date(Date.now() + 86400000 * 3).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-102',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Xây dựng trang Login và lưu Token JWT',
      description: 'Bảo vệ các route /projects và tự động chuyển hướng khi 401.',
      status: 'Done',
      priority: 'High',
      assigneeId: 'u-user-01',
      assigneeName: 'Bảo Anh (Bạn)',
      dueDate: new Date().toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-103',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Tích hợp dự đoán rủi ro AI cho Task',
      description: 'Gọi endpoint GET /api/tasks/{id}/risk, xử lý fallback 503 mượt mà.',
      status: 'Todo',
      priority: 'Medium',
      assigneeId: 'u-user-04',
      assigneeName: 'Phương Thảo',
      dueDate: new Date(Date.now() + 86400000 * 5).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'task-104',
      projectId: 'e1d2c3b4-a5f6-4789-8012-3456789abcde',
      title: 'Kiểm tra Responsive trên thiết bị di động',
      description: 'Đảm bảo thanh Kanban trượt ngang mượt mà trên màn hình nhỏ.',
      status: 'Todo',
      priority: 'Low',
      assigneeId: 'u-user-03',
      assigneeName: 'Hải Đăng',
      dueDate: new Date(Date.now() + 86400000 * 7).toISOString().slice(0, 10),
      createdAt: new Date().toISOString(),
    },
  ]

  const initialRisks: Record<string, TaskRiskResponse> = {
    'task-101': { taskId: 'task-101', risk: 'medium', source: 'ai-service' },
    'task-102': { taskId: 'task-102', risk: 'low', source: 'ai-service' },
    'task-103': { taskId: 'task-103', risk: 'high', source: 'ai-service' },
    'task-104': { taskId: 'task-104', risk: 'low', source: 'ai-service' },
  }

  return {
    projects: initialProjects,
    tasks: initialTasks,
    risks: initialRisks,
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
    if (!project) throw new Error('Không tìm thấy dự án')
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

  getTasks(projectId: string): TaskItem[] {
    const db = loadDB()
    return db.tasks.filter((t) => t.projectId === projectId)
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

  getTaskRisk(taskId: string): TaskRiskResponse {
    const db = loadDB()
    return db.risks[taskId] || { taskId, risk: 'low', source: 'ai-service' }
  },
}
