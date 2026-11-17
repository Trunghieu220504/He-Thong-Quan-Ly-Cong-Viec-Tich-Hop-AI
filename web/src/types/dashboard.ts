// Mirrors ProjectDashboardDto and ActiveSprintOverview from the backend contracts.
export interface ProjectDashboard {
  projectId: string
  generatedAtUtc: string
  tasks: {
    total: number
    todo: number
    inProgress: number
    done: number
    overdue: number
    completionPercent: number
  }
  byAssignee: {
    assigneeId: string | null
    displayName: string
    total: number
    done: number
  }[]
  activeSprint: {
    id: string
    name: string
    startDate: string | null
    endDate: string | null
    totalTasks: number
    completedTasks: number
    remainingTasks: number
  } | null
}
