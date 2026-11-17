import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { ApiError, IS_MOCK } from '../api/client.ts'
import { getProjectDashboard } from '../api/dashboard.ts'
import { getProject, getProjects } from '../api/projects.ts'
import { ProjectDashboard } from '../components/ProjectDashboard.tsx'

export function DashboardError({ error, retry }: { error: unknown; retry: () => void }) {
  const title = error instanceof ApiError && error.status === 403
    ? 'Bạn không có quyền xem báo cáo dự án này'
    : error instanceof ApiError && error.status === 404
      ? 'Không tìm thấy dự án'
      : 'Không thể tải báo cáo'
  return (
    <div className="dashboard-panel dashboard-error" role="alert">
      <h2>{title}</h2>
      <p>{error instanceof Error ? error.message : 'Vui lòng thử lại sau.'}</p>
      <button type="button" className="action-button-neutral" onClick={retry}>Thử lại</button>
    </div>
  )
}

function DashboardProjectPicker() {
  const [page, setPage] = useState(1)
  const query = useQuery({ queryKey: ['projects', page], queryFn: () => getProjects(page), retry: false })
  return (
    <div className="dashboard-page">
      <header className="dashboard-header"><div><h1>Báo cáo tiến độ</h1><p className="dashboard-caption">Chọn dự án để xem tiến độ và phân bổ công việc.</p></div></header>
      {query.isLoading && <p role="status">Đang tải dự án...</p>}
      {query.isError && <DashboardError error={query.error} retry={() => { void query.refetch() }} />}
      {query.data && !query.isError && (
        <>
          {query.data.items.length === 0 ? <p className="dashboard-empty">Chưa có dự án nào. <Link to="/projects">Tạo dự án</Link> để bắt đầu.</p> : (
            <ul className="dashboard-project-list">
              {query.data.items.map((project) => <li key={project.id}><Link to={`/projects/${project.id}/dashboard`}><span>{project.name}</span><span>Xem báo cáo →</span></Link></li>)}
            </ul>
          )}
          {(page > 1 || query.data.items.length < query.data.totalCount) && (
            <nav className="dashboard-pagination" aria-label="Phân trang dự án">
              <button type="button" className="action-button-neutral" disabled={page === 1 || query.isFetching} onClick={() => setPage(page - 1)}>Trang trước</button>
              <span>Trang {page}</span>
              <button type="button" className="action-button-neutral" disabled={page * query.data.pageSize >= query.data.totalCount || query.isFetching} onClick={() => setPage(page + 1)}>Trang sau</button>
            </nav>
          )}
        </>
      )}
    </div>
  )
}

function ProjectReport({ projectId }: { projectId: string }) {
  const project = useQuery({ queryKey: ['project', projectId], queryFn: () => getProject(projectId), retry: false })
  const dashboard = useQuery({
    queryKey: ['dashboard', projectId],
    queryFn: () => getProjectDashboard(projectId),
    refetchInterval: 30000,
    retry: false,
  })
  // A failed refresh must not leave old statistics presented as current.
  const error = dashboard.error || project.error
  return (
    <div className="dashboard-page">
      <nav className="breadcrumbs-navigation" aria-label="Đường dẫn báo cáo"><Link to="/dashboard">Báo cáo tiến độ</Link><span>/</span><Link to={`/projects/${projectId}`}>{project.data?.name || 'Dự án'}</Link></nav>
      <header className="dashboard-header">
        <div>
          <h1>Tổng quan dự án{project.data ? ` · ${project.data.name}` : ''}</h1>
          <p className="dashboard-caption">{IS_MOCK ? 'Dữ liệu demo lưu trên trình duyệt.' : 'Tiến độ toàn bộ công việc và Sprint đang chạy.'}</p>
          {dashboard.data && !error && <p className="dashboard-caption">Cập nhật lúc {new Date(dashboard.data.generatedAtUtc).toLocaleString('vi-VN')} · Tự làm mới mỗi 30 giây</p>}
        </div>
        <div className="dashboard-actions">
          <Link className="action-button-neutral" to={`/projects/${projectId}`}>Bảng công việc</Link>
          <button type="button" className="primary-accent-btn" disabled={dashboard.isFetching || project.isFetching} onClick={() => { void dashboard.refetch(); void project.refetch() }}>Làm mới</button>
        </div>
      </header>
      {error ? <DashboardError error={error} retry={() => { void dashboard.refetch(); void project.refetch() }} />
        : dashboard.isLoading || project.isLoading ? <p role="status">Đang tải báo cáo dự án...</p>
          : dashboard.data && <ProjectDashboard data={dashboard.data} />}
    </div>
  )
}

export function ProjectDashboardPage() {
  const { projectId } = useParams<{ projectId: string }>()
  return projectId ? <ProjectReport key={projectId} projectId={projectId} /> : <DashboardProjectPicker />
}
