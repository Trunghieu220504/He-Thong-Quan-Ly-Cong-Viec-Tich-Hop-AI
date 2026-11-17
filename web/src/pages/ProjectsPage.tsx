import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getProjects } from '../api/projects.ts'
import { CreateProjectModal } from '../components/CreateProjectModal.tsx'
import { PlusIcon, SearchIcon, UsersGroupIcon } from '../components/AppIcons.tsx'

export function ProjectsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [search, setSearch] = useState('')

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['projects'],
    queryFn: () => getProjects(),
  })

  const projects = data?.items || []
  const filtered = projects.filter((p) =>
    p.name.toLowerCase().includes(search.toLowerCase()),
  )

  return (
    <div className="board-page-main-wrapper">
      <div className="space-header-container">
        <div className="breadcrumbs-navigation">
          <span>Spaces</span>
          <span className="breadcrumb-separator">/</span>
        </div>

        <div className="space-headline-row">
          <div className="space-identity-box">
            <h1 className="space-primary-title">Danh sách Spaces & Dự Án</h1>
          </div>
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="primary-accent-btn"
          >
            <PlusIcon />
            <span>Tạo Space mới</span>
          </button>
        </div>

        <div style={{ marginTop: '14px', maxWidth: '320px' }}>
          <div className="board-search-container">
            <SearchIcon className="search-icon-inside" />
            <input
              type="text"
              placeholder="Tìm kiếm dự án..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="board-search-input"
            />
          </div>
        </div>
      </div>

      <div style={{ padding: '0 24px 24px 24px' }}>
        {isLoading && (
          <div className="status-state-box">
            <div className="spinner" />
            <p className="state-description-text">Đang tải danh sách spaces...</p>
          </div>
        )}

        {isError && (
          <div className="status-state-box">
            <h2 style={{ color: '#e12238', marginBottom: '8px' }}>
              Không thể tải danh sách dự án
            </h2>
            <p className="state-description-text">
              {error instanceof Error ? error.message : 'Lỗi không xác định'}
            </p>
            <button
              type="button"
              onClick={() => refetch()}
              className="action-button-neutral"
            >
              Thử lại
            </button>
          </div>
        )}

        {!isLoading && !isError && projects.length === 0 && (
          <div className="status-state-box">
            <h2 style={{ fontSize: '1.2rem', marginBottom: '8px' }}>Chưa có dự án nào</h2>
            <p className="state-description-text">
              Tạo không gian dự án đầu tiên để trải nghiệm bảng Kanban PMAI tích hợp AI.
            </p>
            <button
              type="button"
              onClick={() => setIsModalOpen(true)}
              className="primary-accent-btn"
            >
              <PlusIcon />
              <span>Tạo dự án ngay</span>
            </button>
          </div>
        )}

        {!isLoading && !isError && projects.length > 0 && (
          <div className="projects-card-grid">
            {filtered.map((proj, idx) => {
              const colors = ['bg-jira-redblue', 'bg-jira-purple', 'bg-jira-orange', 'bg-blue']
              const colorClass = colors[idx % colors.length]

              return (
                <Link
                  key={proj.id}
                  to={`/projects/${proj.id}`}
                  className="project-display-card"
                >
                  <div className="project-card-header-row">
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      <span className={`space-avatar-circle ${colorClass}`}>
                        {proj.name.charAt(0).toUpperCase()}
                      </span>
                      <div>
                        <h2 className="project-headline-text">{proj.name}</h2>
                        <span className="project-key-label">PMS-{150 + idx}</span>
                      </div>
                    </div>
                    <span className={`role-badge-tag role-${proj.myRole}`}>
                      {proj.myRole}
                    </span>
                  </div>

                  <p className="project-card-description">
                    {proj.description || 'Chưa có mô tả cho không gian làm việc này.'}
                  </p>

                  <div className="project-card-footer-info">
                    <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <UsersGroupIcon style={{ width: '14px', height: '14px' }} />
                      {proj.memberCount || 1} thành viên
                    </span>
                    <span>
                      {new Date(proj.createdAt).toLocaleDateString('vi-VN')}
                    </span>
                  </div>
                </Link>
              )
            })}
          </div>
        )}
      </div>

      <CreateProjectModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </div>
  )
}
