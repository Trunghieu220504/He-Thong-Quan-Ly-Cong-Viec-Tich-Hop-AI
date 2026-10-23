import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createTask } from '../api/tasks.ts'
import type { ProjectMember } from '../types/project.ts'
import type { TaskPriority } from '../types/task.ts'

interface Props {
  isOpen: boolean
  onClose: () => void
  projectId: string
  members: ProjectMember[]
}

export function CreateTaskModal({ isOpen, onClose, projectId, members }: Props) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [assigneeId, setAssigneeId] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [error, setError] = useState<string | null>(null)

  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () =>
      createTask(projectId, {
        title,
        description: description.trim() ? description : undefined,
        priority,
        assigneeId: assigneeId.trim() ? assigneeId : undefined,
        dueDate: dueDate.trim() ? dueDate : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] })
      setTitle('')
      setDescription('')
      setPriority('Medium')
      setAssigneeId('')
      setDueDate('')
      setError(null)
      onClose()
    },
    onError: (err: Error) => {
      setError(err.message || 'Không thể tạo công việc mới.')
    },
  })

  if (!isOpen) return null

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!title.trim()) return
    setError(null)
    mutation.mutate()
  }

  return (
    <div className="modal-backdrop">
      <div className="modal-content">
        <div className="modal-header">
          <h2>Tạo công việc mới</h2>
          <button type="button" onClick={onClose} className="btn btn-secondary btn-sm">
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div className="alert alert-error">
                <span>{error}</span>
              </div>
            )}

            <div className="form-group">
              <label className="form-label" htmlFor="task-title">
                Tiêu đề công việc *
              </label>
              <input
                id="task-title"
                type="text"
                className="form-input"
                placeholder="VD: Viết tài liệu API Swagger"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="task-desc">
                Mô tả chi tiết
              </label>
              <textarea
                id="task-desc"
                className="form-textarea"
                placeholder="Chi tiết yêu cầu, tiêu chí nghiệm thu..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group">
                <label className="form-label" htmlFor="task-priority">
                  Mức độ ưu tiên
                </label>
                <select
                  id="task-priority"
                  className="form-select"
                  value={priority}
                  onChange={(e) => setPriority(e.target.value as TaskPriority)}
                >
                  <option value="Low">Thấp (Low)</option>
                  <option value="Medium">Trung bình (Medium)</option>
                  <option value="High">Khẩn cấp (High)</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="task-assignee">
                  Người thực hiện
                </label>
                <select
                  id="task-assignee"
                  className="form-select"
                  value={assigneeId}
                  onChange={(e) => setAssigneeId(e.target.value)}
                >
                  <option value="">-- Chưa gán ai --</option>
                  {members.map((m) => (
                    <option key={m.userId} value={m.userId}>
                      {m.displayName || m.userId} ({m.role})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="task-due">
                Hạn hoàn thành (Due date)
              </label>
              <input
                id="task-due"
                type="date"
                className="form-input"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="btn btn-secondary">
              Hủy
            </button>
            <button type="submit" className="btn btn-primary" disabled={mutation.isPending}>
              {mutation.isPending ? 'Đang tạo...' : 'Tạo công việc'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
