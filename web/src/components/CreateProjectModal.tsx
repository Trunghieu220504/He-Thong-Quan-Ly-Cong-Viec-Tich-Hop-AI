import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createProject } from '../api/projects.ts'

interface Props {
  isOpen: boolean
  onClose: () => void
}

export function CreateProjectModal({ isOpen, onClose }: Props) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)

  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => createProject({ name, description }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      setName('')
      setDescription('')
      setError(null)
      onClose()
    },
    onError: (err: Error) => {
      setError(err.message || 'Không thể tạo dự án.')
    },
  })

  if (!isOpen) return null

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!name.trim()) return
    setError(null)
    mutation.mutate()
  }

  return (
    <div className="modal-backdrop">
      <div className="modal-content">
        <div className="modal-header">
          <h2>Tạo dự án mới</h2>
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
              <label className="form-label" htmlFor="proj-name">
                Tên dự án *
              </label>
              <input
                id="proj-name"
                type="text"
                className="form-input"
                placeholder="VD: Quản lý chiến dịch tiếp thị"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                maxLength={200}
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="proj-desc">
                Mô tả chi tiết
              </label>
              <textarea
                id="proj-desc"
                className="form-textarea"
                placeholder="Mô tả mục tiêu, phạm vi và thành phần của dự án..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={2000}
              />
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" onClick={onClose} className="btn btn-secondary">
              Hủy
            </button>
            <button type="submit" className="btn btn-primary" disabled={mutation.isPending}>
              {mutation.isPending ? 'Đang tạo...' : 'Tạo dự án'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
