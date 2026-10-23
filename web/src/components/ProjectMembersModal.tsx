import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addProjectMember } from '../api/projects.ts'
import type { ProjectMember, ProjectRole } from '../types/project.ts'

interface Props {
  isOpen: boolean
  onClose: () => void
  projectId: string
  members: ProjectMember[]
  myRole: ProjectRole
}

const PRESET_USERS = [
  { userId: 'u-user-02', displayName: 'Trung Hiếu' },
  { userId: 'u-user-03', displayName: 'Hải Đăng' },
  { userId: 'u-user-04', displayName: 'Phương Thảo' },
  { userId: 'u-user-05', displayName: 'Tuấn Anh' },
  { userId: 'u-user-06', displayName: 'Hòa Nguyễn' },
]

export function ProjectMembersModal({ isOpen, onClose, projectId, members, myRole }: Props) {
  const [selectedUser, setSelectedUser] = useState('')
  const [customUserId, setCustomUserId] = useState('')
  const [customName, setCustomName] = useState('')
  const [role, setRole] = useState<ProjectRole>('Member')
  const [error, setError] = useState<string | null>(null)

  const queryClient = useQueryClient()
  const canManageMembers = myRole === 'Owner' || myRole === 'Admin'

  const mutation = useMutation({
    mutationFn: (data: { userId: string; role: ProjectRole; displayName?: string }) =>
      addProjectMember(projectId, {
        userId: data.userId,
        role: data.role,
        displayName: data.displayName,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['project', projectId] })
      setSelectedUser('')
      setCustomUserId('')
      setCustomName('')
      setError(null)
    },
    onError: (err: Error) => {
      setError(err.message || 'Không thể thêm thành viên vào dự án.')
    },
  })

  if (!isOpen) return null

  const handleAddMember = (e: FormEvent) => {
    e.preventDefault()
    setError(null)

    let userId = customUserId.trim()
    let displayName = customName.trim()

    if (selectedUser) {
      const preset = PRESET_USERS.find((u) => u.userId === selectedUser)
      if (preset) {
        userId = preset.userId
        displayName = preset.displayName
      }
    }

    if (!userId) {
      setError('Vui lòng chọn hoặc nhập mã/tên người dùng.')
      return
    }

    const alreadyIn = members.some((m) => m.userId === userId)
    if (alreadyIn) {
      setError('Thành viên này đã có trong dự án.')
      return
    }

    mutation.mutate({
      userId,
      role,
      displayName: displayName || userId,
    })
  }

  return (
    <div className="modal-backdrop">
      <div className="modal-content" style={{ maxWidth: '560px' }}>
        <div className="modal-header">
          <h2>Thành viên Dự án ({members.length})</h2>
          <button type="button" onClick={onClose} className="btn btn-secondary btn-sm">
            ✕
          </button>
        </div>

        <div className="modal-body">
          {error && (
            <div className="alert alert-error">
              <span>{error}</span>
            </div>
          )}

          {canManageMembers && (
            <form onSubmit={handleAddMember} style={{ marginBottom: '24px', paddingBottom: '20px', borderBottom: '1px solid var(--border)' }}>
              <h3 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: '12px' }}>
                + Thêm thành viên mới
              </h3>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '10px' }}>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" htmlFor="select-user">
                    Chọn nhanh thành viên
                  </label>
                  <select
                    id="select-user"
                    className="form-select"
                    value={selectedUser}
                    onChange={(e) => {
                      setSelectedUser(e.target.value)
                      if (e.target.value) {
                        setCustomUserId('')
                        setCustomName('')
                      }
                    }}
                  >
                    <option value="">-- Chọn từ gợi ý --</option>
                    {PRESET_USERS.map((u) => (
                      <option key={u.userId} value={u.userId}>
                        {u.displayName} ({u.userId})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" htmlFor="member-role">
                    Vai trò trong dự án
                  </label>
                  <select
                    id="member-role"
                    className="form-select"
                    value={role}
                    onChange={(e) => setRole(e.target.value as ProjectRole)}
                  >
                    <option value="Admin">Admin (Quản trị)</option>
                    <option value="Member">Member (Thành viên)</option>
                    <option value="Viewer">Viewer (Quan sát)</option>
                  </select>
                </div>
              </div>

              {!selectedUser && (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginBottom: '10px' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label" htmlFor="custom-user-id">
                      Hoặc nhập User ID
                    </label>
                    <input
                      id="custom-user-id"
                      type="text"
                      className="form-input"
                      placeholder="VD: u-user-99"
                      value={customUserId}
                      onChange={(e) => setCustomUserId(e.target.value)}
                    />
                  </div>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label" htmlFor="custom-user-name">
                      Họ và tên
                    </label>
                    <input
                      id="custom-user-name"
                      type="text"
                      className="form-input"
                      placeholder="VD: Nguyễn Văn A"
                      value={customName}
                      onChange={(e) => setCustomName(e.target.value)}
                    />
                  </div>
                </div>
              )}

              <button
                type="submit"
                className="btn btn-primary btn-sm"
                style={{ marginTop: '8px' }}
                disabled={mutation.isPending}
              >
                {mutation.isPending ? 'Đang thêm...' : '+ Thêm vào dự án'}
              </button>
            </form>
          )}

          <h3 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: '12px' }}>
            Danh sách thành viên hiện tại
          </h3>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '220px', overflowY: 'auto' }}>
            {members.map((m) => (
              <div
                key={m.userId}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '8px 12px',
                  background: 'var(--bg-subtle)',
                  borderRadius: '8px',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div className="avatar" style={{ width: '28px', height: '28px', fontSize: '0.75rem' }}>
                    {(m.displayName || m.userId).charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '0.875rem' }}>
                      {m.displayName || m.userId}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-light)' }}>
                      ID: {m.userId}
                    </div>
                  </div>
                </div>
                <span className={`role-badge role-${m.role}`}>
                  {m.role}
                </span>
              </div>
            ))}
          </div>
        </div>

        <div className="modal-footer">
          <button type="button" onClick={onClose} className="btn btn-secondary">
            Đóng
          </button>
        </div>
      </div>
    </div>
  )
}
