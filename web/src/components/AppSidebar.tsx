import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getProjects } from '../api/projects.ts'
import {
  AppsIcon,
  ChartIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ClockIcon,
  ExportIcon,
  FilterIcon,
  PlusIcon,
  SlidersIcon,
  StarIcon,
  UserIcon,
  UsersGroupIcon,
} from './AppIcons.tsx'

interface Props {
  onOpenCreateProject?: () => void
}

// Băm tên dự án thành màu avatar ngẫu nhiên nhưng nhất quán
function getProjectColorClass(name: string): string {
  const colors = ['bg-jira-redblue', 'bg-jira-purple', 'bg-jira-orange', 'bg-blue', 'avatar-green', 'avatar-cyan']
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = (hash << 5) - hash + name.charCodeAt(i)
    hash |= 0
  }
  return colors[Math.abs(hash) % colors.length]
}

export function AppSidebar({ onOpenCreateProject }: Props) {
  const [isCollapsed, setIsCollapsed] = useState(false)
  const location = useLocation()

  // Lấy danh sách dự án thực tế từ API/Store
  const { data: projectsData } = useQuery({
    queryKey: ['projects'],
    queryFn: () => getProjects(),
    staleTime: 60 * 1000,
  })

  const projects = projectsData?.items || []
  const activeProjectId = location.pathname.startsWith('/projects/')
    ? location.pathname.split('/')[2]
    : null

  if (isCollapsed) {
    return (
      <aside className="sidebar-collapsed">
        <button
          type="button"
          onClick={() => setIsCollapsed(false)}
          className="sidebar-toggle-btn"
          title="Mở rộng menu bên"
        >
          <ChevronRightIcon />
        </button>

        <div className="sidebar-collapsed-icons">
          <Link to="/projects" className="sidebar-collapsed-item" title="Không gian làm việc (Spaces)">
            <span className="space-avatar-circle bg-blue">S</span>
          </Link>
          <button type="button" className="sidebar-collapsed-item" title="Bộ lọc">
            <FilterIcon />
          </button>
          <button type="button" className="sidebar-collapsed-item" title="Bảng điều khiển">
            <ChartIcon />
          </button>
          <button type="button" className="sidebar-collapsed-item" title="Đội ngũ">
            <UsersGroupIcon />
          </button>
        </div>
      </aside>
    )
  }

  return (
    <aside className="app-sidebar">
      <div className="sidebar-inner">
        {/* Nhóm menu cá nhân */}
        <div className="sidebar-nav-group">
          <button type="button" className="sidebar-nav-item">
            <UserIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Dành cho bạn</span>
          </button>
          <button type="button" className="sidebar-nav-item">
            <ClockIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Gần đây</span>
            <ChevronRightIcon className="sidebar-nav-chevron" />
          </button>
          <button type="button" className="sidebar-nav-item">
            <StarIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Đã đánh dấu sao</span>
            <ChevronRightIcon className="sidebar-nav-chevron" />
          </button>
          <button type="button" className="sidebar-nav-item">
            <AppsIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Ứng dụng mở rộng</span>
          </button>
        </div>

        <div className="sidebar-divider" />

        {/* Nhóm Spaces / Projects */}
        <div className="sidebar-nav-section">
          <div className="sidebar-section-header">
            <span className="sidebar-section-title">Không gian dự án ({projects.length})</span>
            <div className="sidebar-section-actions">
              <button
                type="button"
                onClick={onOpenCreateProject}
                className="icon-btn-ghost"
                title="Tạo Space mới"
              >
                <PlusIcon />
              </button>
              <Link to="/projects" className="icon-btn-ghost" title="Tất cả dự án">
                •••
              </Link>
            </div>
          </div>

          <div className="sidebar-subsection-title">Gần đây</div>

          <div className="spaces-list">
            {projects.length > 0 ? (
              projects.map((proj) => {
                const isActive = proj.id === activeProjectId
                const colorClass = getProjectColorClass(proj.name)

                return (
                  <Link
                    key={proj.id}
                    to={`/projects/${proj.id}`}
                    className={`space-item ${isActive ? 'active' : ''}`}
                    title={`${proj.name} (${proj.memberCount || 1} thành viên)`}
                  >
                    <span className={`space-avatar-circle ${colorClass}`}>
                      {proj.name.charAt(0).toUpperCase()}
                    </span>
                    <span className="space-name">{proj.name}</span>
                  </Link>
                )
              })
            ) : (
              <div className="sidebar-empty-state">
                Chưa có không gian dự án nào
              </div>
            )}

            <Link to="/projects" className="sidebar-nav-sublink">
              <span>Xem tất cả spaces ({projects.length})</span>
              <ChevronRightIcon />
            </Link>
          </div>

          <div className="sidebar-subsection-title" style={{ marginTop: '14px' }}>Khám phá thêm</div>
          <button
            type="button"
            onClick={onOpenCreateProject}
            className="space-item roadmap-item"
          >
            <span className="roadmap-icon">🗺️</span>
            <span className="space-name">Lộ trình Roadmap</span>
            <span className="badge-pill">Mới</span>
          </button>
        </div>

        <div className="sidebar-divider" />

        {/* Menu liên kết điều hướng khác */}
        <div className="sidebar-nav-group">
          <button type="button" className="sidebar-nav-item">
            <FilterIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Bộ lọc công việc</span>
          </button>
          <button type="button" className="sidebar-nav-item">
            <ChartIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Bảng báo cáo tiến độ</span>
          </button>
          <button type="button" className="sidebar-nav-item">
            <AppsIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Tài nguyên dự án</span>
            <ExportIcon className="sidebar-nav-external" />
          </button>
          <button type="button" className="sidebar-nav-item">
            <UsersGroupIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Nhóm làm việc</span>
          </button>
        </div>

        {/* Footer sidebar */}
        <div className="sidebar-footer">
          <button type="button" className="sidebar-nav-item">
            <SlidersIcon className="sidebar-nav-icon" />
            <span className="sidebar-nav-label">Tùy biến thanh bên</span>
          </button>
        </div>
      </div>

      <button
        type="button"
        onClick={() => setIsCollapsed(true)}
        className="sidebar-collapse-edge-btn"
        title="Thu gọn sidebar"
      >
        <ChevronLeftIcon />
      </button>
    </aside>
  )
}
