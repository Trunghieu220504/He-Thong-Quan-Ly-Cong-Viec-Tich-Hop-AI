import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { AddMemberRequest, CreateProjectRequest, ProjectDetail, ProjectsListResponse } from '../types/project.ts'

export async function getProjects(page = 1, pageSize = 20): Promise<ProjectsListResponse> {
  if (IS_MOCK) {
    return mockService.getProjects()
  }

  try {
    return await apiClient<ProjectsListResponse>(`/api/projects?page=${page}&pageSize=${pageSize}`)
  } catch (error) {
    // Nếu chưa chạy backend, tự động fallback sang mock để không nghẽn luồng xem giao diện
    console.warn('Backend chưa sẵn sàng, dùng tạm mock data:', error)
    return mockService.getProjects()
  }
}

export async function getProject(id: string): Promise<ProjectDetail> {
  if (IS_MOCK) {
    return mockService.getProject(id)
  }

  try {
    return await apiClient<ProjectDetail>(`/api/projects/${id}`)
  } catch (error) {
    console.warn(`Không lấy được dự án ${id} từ backend, dùng tạm mock data:`, error)
    return mockService.getProject(id)
  }
}

export async function createProject(data: CreateProjectRequest): Promise<{ id: string }> {
  if (IS_MOCK) {
    return mockService.createProject(data.name, data.description)
  }

  try {
    return await apiClient<{ id: string }>('/api/projects', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  } catch (error) {
    console.warn('Backend tạo dự án lỗi, tạo trên mock data:', error)
    return mockService.createProject(data.name, data.description)
  }
}

export async function addProjectMember(
  projectId: string,
  data: AddMemberRequest & { displayName?: string },
): Promise<void> {
  if (IS_MOCK) {
    mockService.addProjectMember(projectId, data)
    return
  }

  try {
    await apiClient<void>(`/api/projects/${projectId}/members`, {
      method: 'POST',
      body: JSON.stringify({ userId: data.userId, role: data.role }),
    })
  } catch (error) {
    console.warn('Backend thêm thành viên lỗi, lưu vào mock:', error)
    mockService.addProjectMember(projectId, data)
  }
}

