export type ProjectRole = 'Owner' | 'Admin' | 'Member' | 'Viewer'

export interface ProjectSummary {
  id: string
  name: string
  description?: string
  ownerId: string
  myRole: ProjectRole
  memberCount: number
  createdAt: string
}

export interface ProjectMember {
  userId: string
  role: ProjectRole
  joinedAt: string
  displayName?: string
}

export interface ProjectDetail {
  id: string
  name: string
  description?: string
  ownerId: string
  myRole: ProjectRole
  createdAt: string
  updatedAt?: string
  members: ProjectMember[]
}

export interface CreateProjectRequest {
  name: string
  description?: string
}

export interface AddMemberRequest {
  userId: string
  role: ProjectRole
}

export interface ProjectsListResponse {
  items: ProjectSummary[]
  page: number
  pageSize: number
  totalCount: number
}
