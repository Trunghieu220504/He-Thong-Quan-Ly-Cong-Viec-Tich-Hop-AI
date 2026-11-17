import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getTaskRisk } from '../api/risk.ts'
import { updateTask } from '../api/tasks.ts'
import type { ProjectMember } from '../types/project.ts'
import type { TaskItem, TaskPriority, TaskStatus } from '../types/task.ts'
import {
  BookmarkIcon,
  CloseIcon,
  PriorityDownIcon,
  PriorityEqualIcon,
  PriorityUpIcon,
  RobotIcon,
} from './AppIcons.tsx'

interface Props {
  isOpen: boolean
  task: TaskItem | null
  members: ProjectMember[]
  isViewer?: boolean
  onClose: () => void
}

function getTaskKey(taskId: string): string {
  const digits = taskId.replace(/\D/g, '')
  if (digits.length >= 3) {
    return `PMS-${digits.slice(-3)}`
  }
  let hash = 0
  for (let i = 0; i < taskId.length; i++) {
    hash = (hash << 5) - hash + taskId.charCodeAt(i)
    hash |= 0
  }
  const num = 140 + Math.abs(hash % 30)
  return `PMS-${num}`
}

export function TaskDetailModal({ isOpen, task, members, isViewer = false, onClose }: Props) {
  const queryClient = useQueryClient()

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [status, setStatus] = useState<TaskStatus>('Todo')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [assigneeId, setAssigneeId] = useState<string>('')
  const [dueDate, setDueDate] = useState<string>('')
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  // Điền dữ liệu khi mở modal hoặc đổi task
  useEffect(() => {
    if (task) {
      setTitle(task.title || '')
      setDescription(task.description || '')
      setStatus(task.status || 'Todo')
      setPriority(task.priority || 'Medium')
      setAssigneeId(task.assigneeId || '')
      setDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '')
      setErrorMsg(null)
      setSuccessMsg(null)
    }
  }, [task])

  // Lấy đánh giá rủi ro từ Risk AI
  const { data: riskData, isLoading: isRiskLoading } = useQuery({
    queryKey: ['task-risk', task?.id],
    queryFn: () => (task?.id ? getTaskRisk(task.id) : null),
    enabled: isOpen && !!task?.id,
    staleTime: 60 * 1000,
  })

  // Mutation cập nhật thông tin task
  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!task) return
      await updateTask(task.id, {
        title: title.trim(),
        description: description.trim() || undefined,
        status,
        priority,
        assigneeId: assigneeId || undefined,
        dueDate: dueDate || undefined,
      })
    },
    onSuccess: () => {
      setSuccessMsg('Đã lưu thay đổi thành công!')
      queryClient.invalidateQueries({ queryKey: ['tasks', task?.projectId] })
      queryClient.invalidateQueries({ queryKey: ['task-risk', task?.id] })
      setTimeout(() => {
        onClose()
      }, 600)
    },
    onError: (err) => {
      setErrorMsg(err instanceof Error ? err.message : 'Không thể lưu công việc')
    },
  })

  if (!isOpen || !task) return null

  const taskKey = getTaskKey(task.id)

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault()
    if (!title.trim()) {
      setErrorMsg('Vui lòng nhập tiêu đề công việc')
      return
    }
    setErrorMsg(null)
    updateMutation.mutate()
  }

  const renderRiskSection = () => {
    if (isRiskLoading) {
      return (
        <div className="risk-evaluation-box loading">
          <RobotIcon className="robot-icon spin" />
          <span>Đang phân tích rủi ro bằng AI...</span>
        </div>
      )
    }

    if (!riskData || !riskData.risk) {
      return (
        <div className="risk-evaluation-box none">
          <RobotIcon className="robot-icon" />
          <div>
            <strong>AI: Chưa có dự đoán</strong>
            <p className="risk-desc">Mô hình AI chưa có dữ liệu hoặc dịch vụ dự đoán đang bận.</p>
          </div>
        </div>
      )
    }

    const { risk } = riskData
    if (risk === 'low') {
      return (
        <div className="risk-evaluation-box low">
          <div className="risk-box-header">
            <RobotIcon className="robot-icon text-green" />
            <span className="risk-badge badge-low">RỦI RO THẤP (AN TOÀN)</span>
          </div>
          <p className="risk-desc">
            Tiến độ công việc ổn định, ít nguy cơ chậm trễ. Nguồn: <code>{riskData.source}</code>
          </p>
        </div>
      )
    }

    if (risk === 'medium') {
      return (
        <div className="risk-evaluation-box medium">
          <div className="risk-box-header">
            <RobotIcon className="robot-icon text-yellow" />
            <span className="risk-badge badge-medium">RỦI RO TRUNG BÌNH</span>
          </div>
          <p className="risk-desc">
            Khối lượng công việc cần theo dõi sát sao tiến độ. Nguồn: <code>{riskData.source}</code>
          </p>
        </div>
      )
    }

    return (
      <div className="risk-evaluation-box high">
        <div className="risk-box-header">
          <RobotIcon className="robot-icon text-red" />
          <span className="risk-badge badge-high">RỦI RO CAO (CẢNH BÁO)</span>
        </div>
        <p className="risk-desc">
          Mô hình cảnh báo nguy cơ trễ hạn cao! Nên ưu tiên xử lý hoặc phân bổ thêm nhân lực. Nguồn: <code>{riskData.source}</code>
        </p>
      </div>
    )
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="modal-container task-detail-modal"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        {/* Modal Header */}
        <div className="modal-header">
          <div className="task-detail-header-left">
            <span className="task-key-badge">
              <BookmarkIcon />
              {taskKey}
            </span>
            <span className="task-detail-header-label">Chi tiết & Sửa công việc</span>
          </div>
          <button
            type="button"
            className="modal-close-btn"
            onClick={onClose}
            aria-label="Đóng"
          >
            <CloseIcon />
          </button>
        </div>

        {/* Modal Body / Form */}
        <form onSubmit={handleSave}>
          <div className="modal-body task-detail-body">
            {errorMsg && <div className="pmai-alert pmai-alert-error">{errorMsg}</div>}
            {successMsg && <div className="pmai-alert pmai-alert-success">{successMsg}</div>}
            {isViewer && (
              <div className="pmai-alert pmai-alert-info">
                Bạn đang xem với vai trò Viewer (chỉ đọc). Không thể lưu thay đổi.
              </div>
            )}

            {/* Tiêu đề */}
            <div className="form-group">
              <label htmlFor="task-detail-title">Tiêu đề công việc <span className="required">*</span></label>
              <input
                id="task-detail-title"
                type="text"
                className="pmai-input"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                disabled={isViewer || updateMutation.isPending}
                required
              />
            </div>

            {/* Mô tả */}
            <div className="form-group">
              <label htmlFor="task-detail-desc">Mô tả công việc</label>
              <textarea
                id="task-detail-desc"
                className="pmai-textarea"
                rows={4}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={isViewer || updateMutation.isPending}
                placeholder="Nhập chi tiết các yêu cầu, checklist hoặc tài liệu đính kèm..."
              />
            </div>

            {/* Grid 2 cột cho Status & Priority */}
            <div className="form-grid-row">
              <div className="form-group">
                <label htmlFor="task-detail-status">Trạng thái</label>
                <select
                  id="task-detail-status"
                  className="pmai-select"
                  value={status}
                  onChange={(e) => setStatus(e.target.value as TaskStatus)}
                  disabled={isViewer || updateMutation.isPending}
                >
                  <option value="Todo">To Do (Chưa thực hiện)</option>
                  <option value="InProgress">In Progress (Đang thực hiện)</option>
                  <option value="Done">Done (Hoàn thành)</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="task-detail-priority">Mức ưu tiên</label>
                <div className="priority-select-wrap">
                  <select
                    id="task-detail-priority"
                    className="pmai-select"
                    value={priority}
                    onChange={(e) => setPriority(e.target.value as TaskPriority)}
                    disabled={isViewer || updateMutation.isPending}
                  >
                    <option value="Low">Thấp (Low)</option>
                    <option value="Medium">Trung bình (Medium)</option>
                    <option value="High">Khẩn cấp (High)</option>
                  </select>
                  <div className="priority-icon-indicator">
                    {priority === 'High' && <PriorityUpIcon />}
                    {priority === 'Medium' && <PriorityEqualIcon />}
                    {priority === 'Low' && <PriorityDownIcon />}
                  </div>
                </div>
              </div>
            </div>

            {/* Grid 2 cột cho Assignee & Due Date */}
            <div className="form-grid-row">
              <div className="form-group">
                <label htmlFor="task-detail-assignee">Người phụ trách</label>
                <select
                  id="task-detail-assignee"
                  className="pmai-select"
                  value={assigneeId}
                  onChange={(e) => setAssigneeId(e.target.value)}
                  disabled={isViewer || updateMutation.isPending}
                >
                  <option value="">(Chưa phân công)</option>
                  {members.map((m) => (
                    <option key={m.userId} value={m.userId}>
                      {m.displayName || m.userId} ({m.role})
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="task-detail-due">Hạn hoàn thành (Due Date)</label>
                <input
                  id="task-detail-due"
                  type="date"
                  className="pmai-input"
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  disabled={isViewer || updateMutation.isPending}
                />
              </div>
            </div>

            {/* Phần đánh giá Rủi ro AI (PMS-158) */}
            <div className="form-group">
              <label>Đánh giá rủi ro từ mô hình AI</label>
              {renderRiskSection()}
            </div>
          </div>

          {/* Modal Footer */}
          <div className="modal-footer">
            <button
              type="button"
              className="pmai-btn pmai-btn-subtle"
              onClick={onClose}
              disabled={updateMutation.isPending}
            >
              Hủy
            </button>
            {!isViewer && (
              <button
                type="submit"
                className="pmai-btn pmai-btn-primary"
                disabled={updateMutation.isPending}
              >
                {updateMutation.isPending ? 'Đang lưu...' : 'Lưu thay đổi'}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  )
}

