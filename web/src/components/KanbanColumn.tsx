import { useDroppable } from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { TaskCard } from './TaskCard.tsx'
import type { TaskItem } from '../types/task.ts'
import { CollapseColumnsIcon, ExpandColumnIcon, PlusIcon } from './AppIcons.tsx'

interface Props {
  status: string
  title: string
  tasks: TaskItem[]
  isViewer?: boolean
  isCollapsed?: boolean
  onToggleCollapse?: () => void
  onQuickAdd?: () => void
  onEditTask?: (task: TaskItem) => void
  onChangeStatus?: (taskId: string, status: TaskItem['status']) => void
}

export function KanbanColumn({
  status,
  title,
  tasks,
  isViewer = false,
  isCollapsed = false,
  onToggleCollapse,
  onQuickAdd,
  onEditTask,
  onChangeStatus,
}: Props) {
  const { setNodeRef, isOver } = useDroppable({
    id: status,
    data: {
      type: 'Column',
      status,
    },
  })

  // Trạng thái thu gọn (Collapse)
  if (isCollapsed) {
    return (
      <div
        ref={setNodeRef}
        className={`kanban-column-wrapper collapsed ${isOver ? 'drag-over' : ''}`}
        onClick={onToggleCollapse}
        title={`Mở rộng cột ${title}`}
      >
        <button
          type="button"
          className="column-expand-btn"
          onClick={(e) => {
            e.stopPropagation()
            onToggleCollapse?.()
          }}
          title="Expand"
        >
          <ExpandColumnIcon />
        </button>
        <div className="collapsed-column-vertical-label">
          <span>{title}</span>
          <span className="column-task-counter">{tasks.length}</span>
        </div>
      </div>
    )
  }

  return (
    <div
      ref={setNodeRef}
      className={`kanban-column-wrapper ${isOver ? 'drag-over' : ''}`}
    >
      {/* Tiêu đề cột */}
      <div className="column-header-bar">
        <div className="column-title-box">
          <span className="column-title-text">{title}</span>
          <span className="column-task-counter">{tasks.length}</span>
        </div>

        {onToggleCollapse && (
          <button
            type="button"
            onClick={onToggleCollapse}
            className="column-collapse-btn"
            title="Collapse"
          >
            <CollapseColumnsIcon />
          </button>
        )}
      </div>

      {/* Danh sách thẻ task kéo thả */}
      <div className="column-tasks-scroll-area">
        <SortableContext items={tasks.map((t) => t.id)} strategy={verticalListSortingStrategy}>
          {tasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              isViewer={isViewer}
              onEdit={onEditTask}
              onChangeStatus={onChangeStatus}
            />
          ))}
        </SortableContext>

        {tasks.length === 0 && (
          <div className="column-empty-placeholder">
            <span>Kéo thả công việc vào đây</span>
          </div>
        )}
      </div>

      {/* Nút tạo nhanh task dưới chân cột */}
      {!isViewer && (
        <div className="column-footer-bar">
          <button
            type="button"
            onClick={onQuickAdd}
            className="column-quick-add-btn"
            title="Thêm công việc vào cột này"
          >
            <PlusIcon />
          </button>
        </div>
      )}
    </div>
  )
}
