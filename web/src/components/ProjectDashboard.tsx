import type { ProjectDashboard as DashboardData } from '../types/dashboard.ts'
import './ProjectDashboard.css'

function percent(done: number, total: number) {
  return total > 0 ? Math.round(1000 * done / total) / 10 : 0
}

function Progress({ value, label }: { value: number; label: string }) {
  return (
    <div className="dashboard-progress">
      <progress max={100} value={value} aria-label={label} />
      <span>{value.toLocaleString('vi-VN', { maximumFractionDigits: 1 })}%</span>
    </div>
  )
}

function date(value: string | null) {
  return value ? new Date(value).toLocaleDateString('vi-VN') : 'Chưa đặt'
}

export function ProjectDashboard({ data }: { data: DashboardData }) {
  const { tasks, activeSprint, byAssignee } = data
  const statuses = [
    { label: 'Chưa bắt đầu', count: tasks.todo, color: 'todo' },
    { label: 'Đang thực hiện', count: tasks.inProgress, color: 'in-progress' },
    { label: 'Hoàn thành', count: tasks.done, color: 'done' },
  ]

  return (
    <div className="dashboard-content">
      <dl className="dashboard-metrics">
        {[
          ['Tổng công việc', tasks.total],
          ['Đang thực hiện', tasks.inProgress],
          ['Hoàn thành', tasks.done],
          ['Quá hạn chưa hoàn thành', tasks.overdue],
        ].map(([label, value], index) => (
          <div key={label} className={`dashboard-metric ${index === 3 ? 'dashboard-metric-overdue' : ''}`}>
            <dt>{label}</dt><dd>{value}</dd>
          </div>
        ))}
      </dl>

      {tasks.total === 0 && (
        <p className="dashboard-notice">Dự án chưa có công việc. Tạo công việc từ bảng Kanban để bắt đầu theo dõi tiến độ.</p>
      )}

      <div className="dashboard-panels">
        <section className="dashboard-panel" aria-labelledby="dashboard-progress-title">
          <h2 id="dashboard-progress-title">Tiến độ dự án</h2>
          <p className="dashboard-caption">{tasks.done} / {tasks.total} công việc đã hoàn thành</p>
          <Progress value={tasks.completionPercent} label="Tiến độ hoàn thành dự án" />
          <h3>Phân bổ trạng thái</h3>
          <div className="dashboard-status-bar" aria-hidden="true">
            {statuses.map((status) => (
              <span key={status.color} className={`dashboard-status-${status.color}`} style={{ width: `${percent(status.count, tasks.total)}%` }} />
            ))}
          </div>
          <ul className="dashboard-status-legend">
            {statuses.map((status) => (
              <li key={status.color}>
                <span className={`dashboard-status-dot dashboard-status-${status.color}`} aria-hidden="true" />
                <span>{status.label}</span><strong>{status.count}</strong>
              </li>
            ))}
          </ul>
        </section>

        <section className="dashboard-panel" aria-labelledby="dashboard-sprint-title">
          <h2 id="dashboard-sprint-title">Sprint đang chạy</h2>
          {activeSprint ? (
            <>
              <h3 className="dashboard-sprint-name">{activeSprint.name}</h3>
              <dl className="dashboard-sprint-dates">
                <div><dt>Bắt đầu</dt><dd>{date(activeSprint.startDate)}</dd></div>
                <div><dt>Kết thúc</dt><dd>{date(activeSprint.endDate)}</dd></div>
              </dl>
              <Progress value={percent(activeSprint.completedTasks, activeSprint.totalTasks)} label="Tiến độ Sprint đang chạy" />
              <p className="dashboard-caption">{activeSprint.completedTasks} / {activeSprint.totalTasks} công việc hoàn thành · {activeSprint.remainingTasks} còn lại</p>
            </>
          ) : (
            <p className="dashboard-empty">Chưa có Sprint đang chạy trong dự án này.</p>
          )}
        </section>
      </div>

      <section className="dashboard-panel" aria-labelledby="dashboard-assignees-title">
        <h2 id="dashboard-assignees-title">Công việc theo người thực hiện</h2>
        {byAssignee.length === 0 ? (
          <p className="dashboard-empty">Chưa có dữ liệu phân bổ công việc.</p>
        ) : (
          <div className="dashboard-table-scroll">
            <table className="dashboard-assignees">
              <caption className="dashboard-caption">Thống kê toàn bộ công việc trong dự án, bao gồm công việc chưa giao.</caption>
              <thead><tr><th scope="col">Người thực hiện</th><th scope="col">Tổng</th><th scope="col">Hoàn thành</th><th scope="col">Tiến độ</th></tr></thead>
              <tbody>
                {byAssignee.map((person) => (
                  <tr key={person.assigneeId ?? 'unassigned'}>
                    <th scope="row">{person.assigneeId === null ? 'Chưa giao' : person.displayName}</th>
                    <td>{person.total}</td><td>{person.done}</td>
                    <td><Progress value={percent(person.done, person.total)} label={`Tiến độ của ${person.displayName}`} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
