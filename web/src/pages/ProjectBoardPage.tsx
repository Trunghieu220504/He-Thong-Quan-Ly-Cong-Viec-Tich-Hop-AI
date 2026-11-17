import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getProject } from '../api/projects.ts'
import { getTasks, updateTaskStatus } from '../api/tasks.ts'
import { CreateTaskModal } from '../components/CreateTaskModal.tsx'
import { KanbanColumn } from '../components/KanbanColumn.tsx'
import { ProjectMembersModal } from '../components/ProjectMembersModal.tsx'
import { TaskCard } from '../components/TaskCard.tsx'
import { TaskDetailModal } from '../components/TaskDetailModal.tsx'
import type { TaskItem, TaskStatus } from '../types/task.ts'
import {
  FullscreenIcon,
  PlusIcon,
  SearchIcon,
  ShareIcon,
  SprintIcon,
  UsersGroupIcon,
} from '../components/AppIcons.tsx'
import { useAuthStore } from '../stores/authStore.ts'

interface BoardColumnConfig {
  id: string
  title: string
  isCollapsed?: boolean
  isCustom?: boolean
}

const DEFAULT_COLUMNS: BoardColumnConfig[] = [
  { id: 'Todo', title: 'To Do', isCollapsed: false },
  { id: 'InProgress', title: 'In Progress', isCollapsed: false },
  { id: 'InReview', title: 'In Review', isCollapsed: false },
  { id: 'Done', title: 'Done', isCollapsed: false },
]

export function ProjectBoardPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const queryClient = useQueryClient()
  const { user } = useAuthStore()

  const [activeTab, setActiveTab] = useState<'board' | 'list'>('board')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isMembersOpen, setIsMembersOpen] = useState(false)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [editingTask, setEditingTask] = useState<TaskItem | null>(null)
  const [activeTask, setActiveTask] = useState<TaskItem | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Cấu hình cột linh hoạt theo từng project
  const columnsStorageKey = `pmai_board_cols_${projectId}`
  const [columns, setColumns] = useState<BoardColumnConfig[]>(() => {
    try {
      const raw = localStorage.getItem(columnsStorageKey)
      if (raw) return JSON.parse(raw)
    } catch {}
    return DEFAULT_COLUMNS
  })

  // State thêm cột mới linh hoạt
  const [isAddingColumn, setIsAddingColumn] = useState(false)
  const [newColumnTitle, setNewColumnTitle] = useState('')

  // Bộ lọc linh hoạt: Từ khóa, Thành viên được chọn, Bộ lọc nhanh
  const [searchKeyword, setSearchKeyword] = useState('')
  const [selectedAssigneeId, setSelectedAssigneeId] = useState<string | null>(null)
  const [quickFilter, setQuickFilter] = useState<'all' | 'my' | 'high_priority' | 'high_risk'>('all')

  useEffect(() => {
    if (projectId) {
      localStorage.setItem(columnsStorageKey, JSON.stringify(columns))
    }
  }, [columns, columnsStorageKey, projectId])

  // Lắng nghe sự kiện click nút Create ở top navbar
  useEffect(() => {
    const handleOpenCreate = () => setIsCreateOpen(true)
    window.addEventListener('pmai-open-create-task', handleOpenCreate)
    return () => window.removeEventListener('pmai-open-create-task', handleOpenCreate)
  }, [])

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    }),
  )

  // Lấy chi tiết thông tin dự án
  const {
    data: project,
    isLoading: isProjectLoading,
    isError: isProjectError,
    error: projectError,
  } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => getProject(projectId!),
    enabled: !!projectId,
  })

  // Lấy danh sách task thực tế của dự án
  const {
    data: tasks = [],
    isLoading: isTasksLoading,
    isError: isTasksError,
    error: tasksError,
  } = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => getTasks(projectId!, 1, 100),
    enabled: !!projectId,
  })

  // Mở modal chi tiết & sửa task (PMS-158)
  const handleOpenEdit = (task: TaskItem) => {
    setEditingTask(task)
    setIsDetailOpen(true)
  }

  // Đổi trạng thái nhanh từ dropdown thẻ task (PMS-158)
  const handleChangeStatus = (taskId: string, status: TaskStatus) => {
    statusMutation.mutate({ taskId, status })
  }

  // Mutation cập nhật trạng thái Task
  const statusMutation = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: TaskStatus }) =>
      updateTaskStatus(taskId, status),
    onMutate: async ({ taskId, status }) => {
      setErrorMessage(null)
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] })
      const previousTasks = queryClient.getQueryData<TaskItem[]>(['tasks', projectId]) || []
      queryClient.setQueryData<TaskItem[]>(['tasks', projectId], (old = []) =>
        old.map((t) => (t.id === taskId ? { ...t, status } : t)),
      )
      return { previousTasks }
    },
    onError: (err, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(['tasks', projectId], context.previousTasks)
      }
      setErrorMessage(
        err instanceof Error
          ? `Lỗi cập nhật trạng thái: ${err.message}`
          : 'Không thể cập nhật trạng thái công việc. Đã hoàn tác vị trí.',
      )
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] })
      queryClient.invalidateQueries({ queryKey: ['dashboard', projectId] })
    },
  })

  const isViewer = project?.myRole === 'Viewer'

  // Thu gọn hoặc mở rộng bất kỳ cột nào
  const toggleColumnCollapse = (colId: string) => {
    setColumns((prev) =>
      prev.map((c) => (c.id === colId ? { ...c, isCollapsed: !c.isCollapsed } : c)),
    )
  }

  // Thêm cột mới linh hoạt
  const handleAddColumn = () => {
    const trimmed = newColumnTitle.trim()
    if (!trimmed) return
    const newId = `col-${Date.now()}`
    setColumns((prev) => [...prev, { id: newId, title: trimmed, isCollapsed: false, isCustom: true }])
    setNewColumnTitle('')
    setIsAddingColumn(false)
  }

  // Xóa cột tùy chỉnh
  const handleDeleteColumn = (colId: string) => {
    setColumns((prev) => prev.filter((c) => c.id !== colId))
  }

  // Danh sách avatar thành viên được sinh ĐỘNG từ project.members
  const dynamicMemberList = useMemo(() => {
    const members = project?.members || []
    const palette = ['avatar-navy', 'avatar-cyan', 'avatar-red', 'avatar-green', 'avatar-blue', 'avatar-purple']
    return members.map((m, idx) => {
      const name = m.displayName || m.userId
      const initials = name
        .split(' ')
        .map((w) => w[0])
        .join('')
        .slice(0, 2)
        .toUpperCase()
      return {
        userId: m.userId,
        name,
        role: m.role,
        initials,
        colorClass: palette[idx % palette.length],
      }
    })
  }, [project?.members])

  // Lọc task linh hoạt theo từ khóa, thành viên, và bộ lọc nhanh
  const filteredTasks = useMemo(() => {
    return tasks.filter((t) => {
      if (searchKeyword.trim()) {
        const kw = searchKeyword.toLowerCase()
        const matchTitle = t.title.toLowerCase().includes(kw)
        const matchDesc = t.description?.toLowerCase().includes(kw) || false
        const matchAssignee = t.assigneeName?.toLowerCase().includes(kw) || false
        if (!matchTitle && !matchDesc && !matchAssignee) return false
      }

      if (selectedAssigneeId) {
        if (t.assigneeId !== selectedAssigneeId) return false
      }

      if (quickFilter === 'my') {
        if (t.assigneeId !== user?.id && !t.assigneeName?.includes(user?.displayName || '')) return false
      }
      if (quickFilter === 'high_priority') {
        if (t.priority !== 'High') return false
      }

      return true
    })
  }, [tasks, searchKeyword, selectedAssigneeId, quickFilter, user?.id, user?.displayName])

  // Phân chia task vào các cột linh hoạt
  const columnTaskMap = useMemo(() => {
    const map: Record<string, TaskItem[]> = {}
    columns.forEach((c) => {
      map[c.id] = []
    })

    filteredTasks.forEach((t) => {
      if (t.status === 'Todo' && map.Todo) {
        map.Todo.push(t)
      } else if (t.status === 'Done' && map.Done) {
        map.Done.push(t)
      } else if (t.status === 'InProgress') {
        if (map.InProgress) {
          map.InProgress.push(t)
        } else if (columns[0]) {
          map[columns[0].id]?.push(t)
        }
      } else {
        const firstCol = columns[0]?.id || 'Todo'
        if (map[firstCol]) map[firstCol].push(t)
      }
    })

    return map
  }, [columns, filteredTasks])

  const handleDragStart = (event: DragStartEvent) => {
    if (isViewer) return
    const currentTask = tasks.find((t) => t.id === event.active.id)
    if (currentTask) setActiveTask(currentTask)
  }

  const handleDragEnd = (event: DragEndEvent) => {
    setActiveTask(null)
    if (isViewer) return
    const { active, over } = event
    if (!over) return

    const taskId = String(active.id)
    const task = tasks.find((t) => t.id === taskId)
    if (!task) return

    let targetColId = String(over.id)
    const overTask = tasks.find((t) => t.id === over.id)
    if (overTask) {
      targetColId = overTask.status
    }

    if (!['Todo', 'InProgress', 'Done'].includes(targetColId)) {
      setErrorMessage('Cột demo chỉ lưu bố cục trên trình duyệt, chưa hỗ trợ chuyển công việc.')
      return
    }

    let nextBackendStatus: TaskStatus = 'InProgress'
    if (targetColId === 'Todo') nextBackendStatus = 'Todo'
    else if (targetColId === 'Done') nextBackendStatus = 'Done'
    else nextBackendStatus = 'InProgress'

    if (nextBackendStatus !== task.status) {
      statusMutation.mutate({ taskId, status: nextBackendStatus })
    }
  }

  if (isProjectLoading || isTasksLoading) {
    return (
      <div className="board-page-main-wrapper">
        <div className="status-state-box">
          <div className="spinner" />
          <p className="state-description-text">Đang tải không gian dự án...</p>
        </div>
      </div>
    )
  }

  if (isProjectError || isTasksError || !project) {
    const errorText =
      (projectError instanceof Error ? projectError.message : '') ||
      (tasksError instanceof Error ? tasksError.message : '') ||
      'Không tìm thấy dự án'

    return (
      <div className="board-page-main-wrapper">
        <div className="status-state-box">
          <h2 style={{ color: '#e12238', marginBottom: '8px' }}>Không thể tải dự án</h2>
          <p className="state-description-text">{errorText}</p>
          <Link to="/projects" className="action-button-neutral">
            ← Quay về danh sách dự án
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="board-page-main-wrapper">
      {/* 1. Header khu vực dự án */}
      <div className="space-header-container">
        <div className="breadcrumbs-navigation">
          <Link to="/projects" style={{ textDecoration: 'none', color: 'inherit' }}>
            Spaces
          </Link>
          <span className="breadcrumb-separator">/</span>
          <span>{project.name}</span>
        </div>

        <div className="space-headline-row">
          <div className="space-identity-box">
            <span className="space-color-badge">
              <span className="color-badge-inner" />
            </span>
            <h1 className="space-primary-title">{project.name}</h1>

            <button
              type="button"
              onClick={() => setIsMembersOpen(true)}
              className="members-counter-btn"
              title={`Thành viên: ${project.members?.length || 1} người`}
            >
              <UsersGroupIcon />
            </button>

            <span className={`role-badge-tag role-${project.myRole}`} title="Vai trò của bạn">
              {project.myRole}
            </span>
          </div>

          <div className="space-top-actions">
            <button
              type="button"
              onClick={() => setIsCreateOpen(true)}
              className="primary-accent-btn"
              title="Tạo công việc"
            >
              <PlusIcon />
              <span>Tạo việc</span>
            </button>
            <button type="button" className="ghost-icon-button" title="Chia sẻ">
              <ShareIcon />
            </button>
            <button type="button" className="ghost-icon-button" title="Mở rộng">
              <FullscreenIcon />
            </button>
          </div>
        </div>

        {/* 2. Dải Tabs chuyển đổi chế độ xem linh hoạt */}
        <div className="view-mode-tabs-bar">
          <Link to={`/projects/${project.id}/dashboard`} className="view-mode-tab-item">
            Tổng quan
          </Link>
          <button
            type="button"
            onClick={() => setActiveTab('list')}
            className={`view-mode-tab-item ${activeTab === 'list' ? 'active' : ''}`}
          >
            Danh sách việc (Backlog/List)
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('board')}
            className={`view-mode-tab-item ${activeTab === 'board' ? 'active' : ''}`}
          >
            Bảng Kanban (Board)
          </button>
        </div>
      </div>

      {/* 3. Toolbar bộ lọc & công cụ linh hoạt */}
      <div className="board-toolbar-strip">
        <div className="toolbar-left-controls">
          {/* Ô tìm kiếm bảng board */}
          <div className="board-search-container">
            <SearchIcon className="search-icon-inside" />
            <input
              type="text"
              placeholder="Tìm kiếm công việc..."
              value={searchKeyword}
              onChange={(e) => setSearchKeyword(e.target.value)}
              className="board-search-input"
            />
            {searchKeyword && (
              <button
                type="button"
                onClick={() => setSearchKeyword('')}
                className="search-clear-btn"
              >
                ✕
              </button>
            )}
          </div>

          {/* Bộ lọc Avatar người dùng SINH ĐỘNG từ project.members */}
          <div className="avatar-filters-cluster">
            {dynamicMemberList.map((m) => {
              const isSelected = selectedAssigneeId === m.userId
              return (
                <button
                  key={m.userId}
                  type="button"
                  onClick={() => setSelectedAssigneeId(isSelected ? null : m.userId)}
                  className={`avatar-filter-pill ${m.colorClass} ${isSelected ? 'is-selected' : ''}`}
                  title={`${m.name} (${m.role}) - Nhấn để ${isSelected ? 'bỏ lọc' : 'lọc'}`}
                >
                  {m.initials}
                </button>
              )
            })}
          </div>

          {/* Dropdown bộ lọc nhanh linh hoạt */}
          <select
            className="filter-dropdown-select"
            value={quickFilter}
            onChange={(e) => setQuickFilter(e.target.value as any)}
          >
            <option value="all">Tất cả công việc</option>
            <option value="my">Chỉ việc của tôi</option>
            <option value="high_priority">Việc khẩn cấp (High)</option>
          </select>
        </div>

        <div className="toolbar-right-controls">
          <div className="sprint-status-tag">
            <SprintIcon />
            <span>Sprint 4 (Demo)</span>
          </div>

          {/* Nút thêm cột mới linh hoạt */}
          {activeTab === 'board' && !isViewer && (
            <button
              type="button"
              onClick={() => setIsAddingColumn((prev) => !prev)}
              className="add-column-toggle-btn"
              title="Thêm cột Kanban tùy biến"
            >
              <PlusIcon />
              <span>Thêm cột demo</span>
            </button>
          )}
        </div>
      </div>

      {/* Form tạo thêm cột linh hoạt */}
      {isAddingColumn && (
        <div style={{ margin: '0 24px 14px 24px', padding: '12px', background: '#f4f5f7', borderRadius: '6px', display: 'flex', gap: '10px', alignItems: 'center' }}>
          <input
            type="text"
            placeholder="Nhập tên cột mới (VD: Testing, Đang Review...)"
            value={newColumnTitle}
            onChange={(e) => setNewColumnTitle(e.target.value)}
            className="form-input"
            style={{ maxWidth: '300px' }}
          />
          <button type="button" onClick={handleAddColumn} className="primary-accent-btn">
            Tạo cột
          </button>
          <button type="button" onClick={() => setIsAddingColumn(false)} className="btn btn-secondary btn-sm">
            Hủy
          </button>
        </div>
      )}

      {errorMessage && (
        <div className="system-notice-alert error">
          <span>{errorMessage}</span>
          <button
            type="button"
            onClick={() => setErrorMessage(null)}
            className="alert-close-btn"
          >
            ✕
          </button>
        </div>
      )}

      {tasks.length >= 100 && (
        <div className="system-notice-alert" role="status">
          Chỉ tải tối đa 100 công việc đầu tiên. Danh sách và bộ lọc có thể chưa bao gồm toàn bộ dự án. Xem Tổng quan để có số liệu đầy đủ.
        </div>
      )}
      {activeTab === 'board' && (
        <div className="system-notice-alert" role="status">
          In Review và cột tùy chỉnh là demo, chỉ lưu bố cục trên trình duyệt và không nhận công việc.
        </div>
      )}

      {/* TAB 1: BẢNG KANBAN BOARD */}
      {activeTab === 'board' && (
        <DndContext
          sensors={sensors}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <div className="kanban-columns-container">
            {columns.map((col) => {
              const colTasks = columnTaskMap[col.id] || []
              return (
                <div key={col.id} style={{ position: 'relative' }}>
                  <KanbanColumn
                    status={col.id}
                    title={['Todo', 'InProgress', 'Done'].includes(col.id) ? col.title : col.title + ' (Demo)'}
                    tasks={colTasks}
                    isViewer={isViewer}
                    isCollapsed={col.isCollapsed}
                    onToggleCollapse={() => toggleColumnCollapse(col.id)}
                    onQuickAdd={() => setIsCreateOpen(true)}
                    onEditTask={handleOpenEdit}
                    onChangeStatus={handleChangeStatus}
                  />
                  {col.isCustom && (
                    <button
                      type="button"
                      onClick={() => handleDeleteColumn(col.id)}
                      style={{ position: 'absolute', top: '10px', right: '35px', background: 'none', border: 'none', color: '#8993a4', cursor: 'pointer', fontSize: '0.75rem' }}
                      title="Xóa cột tùy chỉnh này"
                    >
                      ✕
                    </button>
                  )}
                </div>
              )
            })}
          </div>

          <DragOverlay>
            {activeTask ? <TaskCard task={activeTask} isViewer={isViewer} /> : null}
          </DragOverlay>
        </DndContext>
      )}

      {/* TAB 2: DANH SÁCH CÔNG VIỆC DẠNG BẢNG */}
      {activeTab === 'list' && (
        <div style={{ padding: '0 24px 24px 24px' }}>
          <div style={{ background: '#ffffff', border: '1px solid var(--border)', borderRadius: '8px', overflow: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
              <thead style={{ background: '#f4f5f7', borderBottom: '1px solid var(--border)', color: 'var(--text-muted)', fontWeight: 600 }}>
                <tr>
                  <th style={{ padding: '10px 14px' }}>Mã & Tiêu đề công việc</th>
                  <th style={{ padding: '10px 14px' }}>Trạng thái</th>
                  <th style={{ padding: '10px 14px' }}>Mức ưu tiên</th>
                  <th style={{ padding: '10px 14px' }}>Người thực hiện</th>
                  <th style={{ padding: '10px 14px' }}>Hạn hoàn thành</th>
                </tr>
              </thead>
              <tbody>
                {filteredTasks.map((t, idx) => (
                  <tr key={t.id} style={{ borderBottom: '1px solid var(--border)', background: idx % 2 === 0 ? '#ffffff' : '#fafbfc' }}>
                    <td
                      style={{ padding: '10px 14px', fontWeight: 500, cursor: 'pointer' }}
                      onClick={() => handleOpenEdit(t)}
                      title="Nhấn để xem chi tiết & sửa công việc"
                    >
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span style={{ color: '#36b37e' }}>🔖</span>
                        <span style={{ color: '#6b778c', fontSize: '0.78rem' }}>{t.id}</span>
                        <span style={{ color: '#0c66e4', textDecoration: 'none' }}>{t.title}</span>
                      </div>
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      <select
                        value={t.status}
                        onChange={(e) => statusMutation.mutate({ taskId: t.id, status: e.target.value as TaskStatus })}
                        disabled={isViewer}
                        className="form-select"
                        style={{ padding: '3px 8px', fontSize: '0.8rem', width: 'auto' }}
                      >
                        <option value="Todo">To Do</option>
                        <option value="InProgress">In Progress</option>
                        <option value="Done">Done</option>
                      </select>
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      <span className={`priority-badge priority-${t.priority}`}>
                        {t.priority}
                      </span>
                    </td>
                    <td style={{ padding: '10px 14px' }}>
                      {t.assigneeName || 'Chưa gán'}
                    </td>
                    <td style={{ padding: '10px 14px', color: 'var(--text-muted)' }}>
                      {t.dueDate ? new Date(t.dueDate).toLocaleDateString('vi-VN') : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modals */}
      <CreateTaskModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        projectId={project.id}
        members={project.members || []}
      />

      <ProjectMembersModal
        isOpen={isMembersOpen}
        onClose={() => setIsMembersOpen(false)}
        projectId={project.id}
        members={project.members || []}
        myRole={project.myRole}
      />

      {/* PMS-158: Modal chi tiết & sửa task + mức rủi ro AI */}
      <TaskDetailModal
        isOpen={isDetailOpen}
        task={editingTask}
        members={project.members || []}
        isViewer={isViewer}
        onClose={() => {
          setIsDetailOpen(false)
          setEditingTask(null)
        }}
      />
    </div>
  )
}
