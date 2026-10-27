import { apiClient, IS_MOCK } from './client.ts'
import { mockService } from './mockData.ts'
import type { TaskRiskResponse } from '../types/task.ts'

export async function getTaskRisk(taskId: string): Promise<TaskRiskResponse | null> {
  if (IS_MOCK) {
    return mockService.getTaskRisk(taskId)
  }

  try {
    return await apiClient<TaskRiskResponse>(`/api/tasks/${taskId}/risk`)
  } catch {
    // Dịch vụ AI chưa khả dụng hoặc chưa có dự đoán -> trả về null để UI hiển thị 'Chưa có dự đoán'
    return null
  }
}
