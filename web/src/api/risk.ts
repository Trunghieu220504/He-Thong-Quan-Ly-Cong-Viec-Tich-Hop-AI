import { apiClient, ApiError, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { TaskRiskResponse } from '../types/task.ts'

export async function getTaskRisk(taskId: string): Promise<TaskRiskResponse | null> {
  if (IS_MOCK) {
    return mockService.getTaskRisk(taskId)
  }

  try {
    return await apiClient<TaskRiskResponse>(`/api/tasks/${taskId}/risk`)
  } catch (error) {
    // 503 hoặc dịch vụ AI chưa khả dụng -> trả về null để UI hiển thị 'Chưa có dự đoán'
    if (error instanceof ApiError && error.status === 503) {
      return null
    }
    // Fallback sang mock risk nếu backend chưa có endpoint này
    return mockService.getTaskRisk(taskId)
  }
}
