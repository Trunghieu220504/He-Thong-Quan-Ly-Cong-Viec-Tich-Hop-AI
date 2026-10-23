import { useState } from 'react'
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
import type { TaskItem, TaskStatus } from '../types/task.ts'

export function ProjectBoardPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const queryClient = useQueryClient()

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isMembersOpen, setIsMembersOpen] = useState(false)
  const [activeTask, setActiveTask] = useState<TaskItem | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    }),
  )

  // Lấy chi tiết thông tin dự án (bao gồm vai trò myRole và danh sách thành viên)
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

  // Lấy danh sách các task thuộc dự án
  const {
    data: tasks = [],
    isLoading: isTasksLoading,
    isError: isTasksError,
    error: tasksError,
  } = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => getTasks(projectId!),
    enabled: !!projectId,
  })

  // Mutation cập nhật trạng thái Task với Optimistic Update và rollback khi lỗi
  const statusMutation = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: TaskStatus }) =>
      updateTaskStatus(taskId, status),
    onMutate: async ({ taskId, status }) => {
      setErrorMessage(null)
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] })

      const previousTasks = queryClient.getQueryData<TaskItem[]>(['tasks', projectId]) || []

      // Cập nhật giao diện tạm thời (Optimistic)
      queryClient.setQueryData<TaskItem[]>(['tasks', projectId], (old = []) =>
        old.map((t) => (t.id === taskId ? { ...t, status } : t)),
      )

      return { previousTasks }
    },
    onError: (err, _variables, context) => {
      // Hoàn tác lại trạng thái cũ nếu server báo lỗi
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
    },
  })

  const isViewer = project?.myRole === 'Viewer'

  const handleDragStart = (event: DragStartEvent) => {
    if (isViewer) return
    const currentTask = tasks.find((t) => t.id === event.active.id)
    if (currentTask) {
      setActiveTask(currentTask)
    }
  }

  const handleDragEnd = (event: DragEndEvent) => {
    setActiveTask(null)
    if (isViewer) return

    const { active, over } = event
    if (!over) return

    const taskId = String(active.id)
    const task = tasks.find((t) => t.id === taskId)
    if (!task) return

    let targetStatus: TaskStatus | null = null

    // Kiểm tra xem vị trí thả là trực tiếp vào cột hay thả đè lên một task khác
    const validStatuses: TaskStatus[] = ['Todo', 'InProgress', 'Done']
    if (validStatuses.includes(over.id as TaskStatus)) {
      targetStatus = over.id as TaskStatus
    } else {
      const overTask = tasks.find((t) => t.id === over.id)
      if (overTask) {
        targetStatus = overTask.status
      }
    }

    if (targetStatus && targetStatus !== task.status) {
      statusMutation.mutate({ taskId, status: targetStatus })
    }
  }

  if (isProjectLoading || isTasksLoading) {
    return (
      <div className="app-container">
        <div className="state-box">
          <div className="spinner" />
          <p className="state-desc">Đang tải bảng Kanban...</p>
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
      <div className="app-container">
        <div className="state-box">
          <h2 className="state-title" style={{ color: '#b91c1c' }}>
            Không thể tải dự án
          </h2>
          <p className="state-desc">{errorText}</p>
          <Link to="/projects" className="btn btn-secondary">
            ← Quay về danh sách dự án
          </Link>
        </div>
      </div>
    )
  }

  const todoTasks = tasks.filter((t) => t.status === 'Todo')
  const inProgressTasks = tasks.filter((t) => t.status === 'InProgress')
  const doneTasks = tasks.filter((t) => t.status === 'Done')

  return (
    <div className="app-container">
      <div className="board-header">
        <div>
          <div className="board-nav">
            <Link to="/projects" className="btn btn-secondary btn-sm">
              ← Danh sách dự án
            </Link>
            <span className={`role-badge role-${project.myRole}`}>
              Vai trò của bạn: {project.myRole}
            </span>
          </div>
          <h1 style={{ marginTop: '8px' }}>{project.name}</h1>
          <p className="subtitle">{project.description || 'Bảng điều khiển công việc'}</p>
        </div>

        <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
          <button
            type="button"
            onClick={() => setIsMembersOpen(true)}
            className="btn btn-secondary"
          >
            👥 Thành viên ({project.members?.length || 1})
          </button>
          {!isViewer && (
            <button
              type="button"
              onClick={() => setIsCreateOpen(true)}
              className="btn btn-primary"
            >
              + Tạo công việc mới
            </button>
          )}
        </div>
      </div>

      {isViewer && (
        <div className="alert alert-info">
          <span>
            ℹ️ Bạn đang xem dự án với vai trò <strong>Viewer (Người quan sát)</strong>. Bạn chỉ có quyền xem thông tin, không thể tạo hoặc kéo thả thay đổi trạng thái công việc.
          </span>
        </div>
      )}

      {errorMessage && (
        <div className="alert alert-error">
          <span>{errorMessage}</span>
          <button
            type="button"
            onClick={() => setErrorMessage(null)}
            className="btn btn-secondary btn-sm"
          >
            Đóng
          </button>
        </div>
      )}

      <DndContext
        sensors={sensors}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div className="kanban-board">
          <KanbanColumn
            status="Todo"
            title="Cần làm"
            tasks={todoTasks}
            isViewer={isViewer}
          />
          <KanbanColumn
            status="InProgress"
            title="Đang thực hiện"
            tasks={inProgressTasks}
            isViewer={isViewer}
          />
          <KanbanColumn
            status="Done"
            title="Hoàn thành"
            tasks={doneTasks}
            isViewer={isViewer}
          />
        </div>

        <DragOverlay>
          {activeTask ? <TaskCard task={activeTask} isViewer={isViewer} /> : null}
        </DragOverlay>
      </DndContext>

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
    </div>
  )
}
