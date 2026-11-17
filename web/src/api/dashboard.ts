import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { ProjectDashboard } from '../types/dashboard.ts'

export async function getProjectDashboard(projectId: string): Promise<ProjectDashboard> {
  if (IS_MOCK) return mockService.getProjectDashboard(projectId)

  // Aggregate on the server, independently of the Kanban page size and filters.
  return apiClient<ProjectDashboard>(`/api/projects/${projectId}/dashboard`)
}
