import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getProjects } from '../api/projects.ts'
import { CreateProjectModal } from '../components/CreateProjectModal.tsx'

export function ProjectsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false)

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['projects'],
    queryFn: () => getProjects(),
  })

  const projects = data?.items || []

  return (
    <div className="app-container">
      <div className="projects-header">
        <div>
          <h1>Danh sách Dự Án</h1>
          <p className="subtitle">Quản lý tiến độ công việc và phân tích rủi ro AI cho từng dự án</p>
        </div>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="btn btn-primary"
        >
          + Tạo dự án mới
        </button>
      </div>

      {isLoading && (
        <div className="state-box">
          <div className="spinner" />
          <p className="state-desc">Đang tải danh sách dự án...</p>
        </div>
      )}

      {isError && (
        <div className="state-box">
          <h2 className="state-title" style={{ color: '#b91c1c' }}>
            Không thể tải danh sách dự án
          </h2>
          <p className="state-desc">{error instanceof Error ? error.message : 'Lỗi không xác định'}</p>
          <button type="button" onClick={() => refetch()} className="btn btn-secondary">
            Thử lại
          </button>
        </div>
      )}

      {!isLoading && !isError && projects.length === 0 && (
        <div className="state-box">
          <h2 className="state-title">Chưa có dự án nào</h2>
          <p className="state-desc">
            Bắt đầu làm việc bằng cách tạo dự án đầu tiên của bạn để lập kế hoạch Kanban.
          </p>
          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="btn btn-primary"
          >
            + Tạo dự án ngay
          </button>
        </div>
      )}

      {!isLoading && !isError && projects.length > 0 && (
        <div className="projects-grid">
          {projects.map((proj) => (
            <Link
              key={proj.id}
              to={`/projects/${proj.id}`}
              className="project-card"
            >
              <div>
                <div className="project-card-header">
                  <h2 className="project-card-title">{proj.name}</h2>
                  <span className={`role-badge role-${proj.myRole}`}>
                    {proj.myRole}
                  </span>
                </div>
                <p className="project-card-desc">
                  {proj.description || 'Chưa có mô tả cho dự án này.'}
                </p>
              </div>

              <div className="project-card-footer">
                <span>{proj.memberCount || 1} thành viên</span>
                <span>
                  Tạo ngày {new Date(proj.createdAt).toLocaleDateString('vi-VN')}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}

      <CreateProjectModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </div>
  )
}
