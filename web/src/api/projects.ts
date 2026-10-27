import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { AddMemberRequest, CreateProjectRequest, ProjectDetail, ProjectsListResponse } from '../types/project.ts'

export async function getProjects(page = 1, pageSize = 20): Promise<ProjectsListResponse> {
  if (IS_MOCK) {
    return mockService.getProjects()
  }

  return apiClient<ProjectsListResponse>(`/api/projects?page=${page}&pageSize=${pageSize}`)
}

export async function getProject(id: string): Promise<ProjectDetail> {
  if (IS_MOCK) {
    return mockService.getProject(id)
  }

  return apiClient<ProjectDetail>(`/api/projects/${id}`)
}

export async function createProject(data: CreateProjectRequest): Promise<{ id: string }> {
  if (IS_MOCK) {
    return mockService.createProject(data.name, data.description)
  }

  return apiClient<{ id: string }>('/api/projects', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function addProjectMember(
  projectId: string,
  data: AddMemberRequest & { displayName?: string },
): Promise<void> {
  if (IS_MOCK) {
    mockService.addProjectMember(projectId, data)
    return
  }

  await apiClient<void>(`/api/projects/${projectId}/members`, {
    method: 'POST',
    body: JSON.stringify({ userId: data.userId, role: data.role }),
  })
}

