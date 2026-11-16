import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { useQuery } from '@tanstack/react-query'
import { getTaskRisk } from '../api/risk.ts'
import type { TaskItem, TaskStatus } from '../types/task.ts'
import {
  BookmarkIcon,
  CheckmarkIcon,
  ChevronRightIcon,
  EditIcon,
  MoreHorizontalIcon,
  PriorityDownIcon,
  PriorityEqualIcon,
  PriorityUpIcon,
  SubtaskDiagramIcon,
  SubtasksIcon,
} from './AppIcons.tsx'

interface Props {
  task: TaskItem
  isViewer?: boolean
  onEdit?: (task: TaskItem) => void
  onChangeStatus?: (taskId: string, status: TaskStatus) => void
}

// Hàm sinh tag phù hợp theo nội dung task
function getTaskTag(task: TaskItem): string {
  const lowerTitle = task.title.toLowerCase()
  if (lowerTitle.includes('docker') || lowerTitle.includes('infra')) return 'Tuan5-Docker'
  if (lowerTitle.includes('ai') || lowerTitle.includes('ml') || lowerTitle.includes('service')) return 'Tuan5-AI'
  if (lowerTitle.includes('dashboard')) return 'Dashboard'
  if (lowerTitle.includes('web') || lowerTitle.includes('giao diện') || lowerTitle.includes('kanban') || lowerTitle.includes('modal') || lowerTitle.includes('gettasks')) return 'Tuan5-Web'
  return 'Tuan5-Core'
}

// Hàm sinh mã định danh PMS-xxx của nhóm
function getTaskKey(taskId: string): string {
  return taskId
}

function getStoryPoints(task: TaskItem): number {
  if (task.priority === 'High') return 4
  if (task.priority === 'Medium') return 5
  return 3
}

function getAvatarColorClass(name: string): string {
  const first = name.charAt(0).toUpperCase()
  if (['P', 'D'].includes(first)) return 'avatar-green'
  if (['N', 'A', 'B'].includes(first)) return 'avatar-navy'
  if (['H', 'X'].includes(first)) return 'avatar-cyan'
  if (['T'].includes(first)) return 'avatar-blue'
  return 'avatar-purple'
}

export function TaskCard({ task, isViewer = false, onEdit, onChangeStatus }: Props) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: task.id,
    disabled: isViewer,
    data: {
      type: 'Task',
      task,
    },
  })

  // Gọi endpoint Risk AI cho từng task
  const { data: riskData, isLoading: isRiskLoading } = useQuery({
    queryKey: ['task-risk', task.id],
    queryFn: () => getTaskRisk(task.id),
    staleTime: 60 * 1000,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  const renderRiskBadge = () => {
    if (isRiskLoading) {
      return <span className="risk-chip loading" title="Đang tính toán rủi ro...">AI...</span>
    }
    if (!riskData || !riskData.risk) {
      return null
    }

    const { risk } = riskData
    if (risk === 'low') {
      return <span className="risk-chip low" title="AI: Rủi ro thấp">AI: Thấp</span>
    }
    if (risk === 'medium') {
      return <span className="risk-chip medium" title="AI: Rủi ro vừa">AI: Vừa</span>
    }
    if (risk === 'high') {
      return <span className="risk-chip high" title="AI: Rủi ro cao">AI: Cao</span>
    }

    return null
  }

  const taskTag = getTaskTag(task)
  const taskKey = getTaskKey(task.id)
  const storyPoints = getStoryPoints(task)

  const assigneeInitials = task.assigneeName
    ? task.assigneeName
        .split(' ')
        .map((n) => n[0])
        .join('')
        .slice(0, 2)
        .toUpperCase()
    : 'PD'

  const isDone = task.status === 'Done'
  const isSubtask = task.title.startsWith('[W') || taskKey === 'PMS-157' || taskKey === 'PMS-158'

  // Thẻ dạng Subtask (PMS-157, PMS-158) khớp 100% hình ảnh người dùng yêu cầu
  if (isSubtask) {
    return (
      <div
        ref={setNodeRef}
        style={{
          ...style,
          cursor: isViewer ? 'default' : 'grab',
        }}
        {...attributes}
        {...listeners}
        className={`task-issue-card subtask-issue-card ${isDragging ? 'is-dragging' : ''}`}
        onClick={(e) => {
          const target = e.target as HTMLElement
          if (!target.closest('.no-drag-trigger') && !target.closest('select') && !target.closest('button')) {
            onEdit?.(task)
          }
        }}
      >
        {/* Tiêu đề công việc + Nút Sửa nhanh */}
        <div className="task-card-header-line">
          <h3 className="task-issue-title" style={{ fontSize: '0.92rem', lineHeight: '1.45', fontWeight: 500 }}>
            {task.title}
          </h3>
          <div className="task-card-action-icons no-drag-trigger" onClick={(e) => e.stopPropagation()}>
            <button
              type="button"
              className="task-action-btn edit-btn"
              title="Sửa công việc & xem rủi ro AI"
              onClick={() => onEdit?.(task)}
            >
              <EditIcon />
            </button>
            <button
              type="button"
              className="task-action-btn more-btn"
              title="Tùy chọn khác"
              onClick={() => onEdit?.(task)}
            >
              <MoreHorizontalIcon />
            </button>
          </div>
        </div>

        {/* Hàng metadata theo đúng ảnh: [SubtaskDiagramIcon] Key   Status Dropdown   Avatar */}
        <div className="task-issue-meta-row" style={{ marginTop: '12px', alignItems: 'center' }}>
          <div className="task-meta-left-group" style={{ gap: '6px' }}>
            <SubtaskDiagramIcon />
            <span className="task-key-label" style={{ fontWeight: 600, color: '#42526e', fontSize: '0.82rem' }}>
              {taskKey}
            </span>
          </div>

          <div className="task-meta-right-group" style={{ gap: '8px' }}>
            <div className="task-status-pill-wrap no-drag-trigger" onClick={(e) => e.stopPropagation()}>
              <select
                className={`task-status-select-pill pill-${task.status.toLowerCase()}`}
                value={task.status}
                disabled={isViewer}
                onChange={(e) => onChangeStatus?.(task.id, e.target.value as TaskStatus)}
                title="Đổi trạng thái"
              >
                <option value="Todo">To Do ▾</option>
                <option value="InProgress">In Progress ▾</option>
                <option value="Done">Done ▾</option>
              </select>
            </div>

            <div
              className={`member-avatar-circle ${getAvatarColorClass(task.assigneeName || 'NA')}`}
              title={`Người thực hiện: ${task.assigneeName || 'Chưa gán'}`}
            >
              {assigneeInitials}
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div
      ref={setNodeRef}
      style={{
        ...style,
        cursor: isViewer ? 'default' : 'grab',
      }}
      {...attributes}
      {...listeners}
      className={`task-issue-card ${isDragging ? 'is-dragging' : ''}`}
      onClick={(e) => {
        // Mở modal sửa nếu không click vào dropdown hay button
        const target = e.target as HTMLElement
        if (!target.closest('.no-drag-trigger') && !target.closest('select') && !target.closest('button')) {
          onEdit?.(task)
        }
      }}
    >
      {/* Tiêu đề công việc + Nút Sửa nhanh */}
      <div className="task-card-header-line">
        <h3 className="task-issue-title">{task.title}</h3>
        <div className="task-card-action-icons no-drag-trigger" onClick={(e) => e.stopPropagation()}>
          <button
            type="button"
            className="task-action-btn edit-btn"
            title="Sửa công việc & xem rủi ro AI"
            onClick={() => onEdit?.(task)}
          >
            <EditIcon />
          </button>
          <button
            type="button"
            className="task-action-btn more-btn"
            title="Tùy chọn khác"
            onClick={() => onEdit?.(task)}
          >
            <MoreHorizontalIcon />
          </button>
        </div>
      </div>

      {/* Tag nhãn */}
      <div className="task-issue-tag-wrapper">
        <span className={`task-issue-tag tag-${taskTag.toLowerCase().replace(/[^a-z0-9]/g, '-')}`}>
          {taskTag}
        </span>
      </div>

      {/* Hàng metadata: Key, Points, Status Dropdown, Priority, Avatar */}
      <div className="task-issue-meta-row">
        <div className="task-meta-left-group">
          <BookmarkIcon className="task-bookmark-icon" />
          <span className="task-key-label">{taskKey}</span>
          <span className="task-story-points">{storyPoints}</span>

          {isDone && <CheckmarkIcon className="task-done-check" />}

          {task.priority === 'High' && (
            <span title="Ưu tiên: Khẩn cấp" role="img" aria-label="Ưu tiên: Khẩn cấp">
              <PriorityUpIcon aria-hidden="true" />
            </span>
          )}
          {task.priority === 'Medium' && (
            <span title="Ưu tiên: Trung bình" role="img" aria-label="Ưu tiên: Trung bình">
              <PriorityEqualIcon aria-hidden="true" />
            </span>
          )}
          {task.priority === 'Low' && (
            <span title="Ưu tiên: Thấp" role="img" aria-label="Ưu tiên: Thấp">
              <PriorityDownIcon aria-hidden="true" />
            </span>
          )}

          {renderRiskBadge()}
        </div>

        <div className="task-meta-right-group">
          {/* Dropdown đổi status trực tiếp trên thẻ (như Jira trong ảnh PMS-157 / PMS-158) */}
          <div className="task-status-pill-wrap no-drag-trigger" onClick={(e) => e.stopPropagation()}>
            <select
              className={`task-status-select-pill pill-${task.status.toLowerCase()}`}
              value={task.status}
              disabled={isViewer}
              onChange={(e) => onChangeStatus?.(task.id, e.target.value as TaskStatus)}
              title="Đổi trạng thái nhanh"
            >
              <option value="Todo">To Do ▾</option>
              <option value="InProgress">In Progress ▾</option>
              <option value="Done">Done ▾</option>
            </select>
          </div>

          <div
            className={`member-avatar-circle ${getAvatarColorClass(task.assigneeName || 'PD')}`}
            title={`Người thực hiện: ${task.assigneeName || 'Chưa gán'}`}
          >
            {assigneeInitials}
          </div>
        </div>
      </div>

      {/* Thanh Subtasks */}
      <div className="task-subtasks-strip">
        <div className="task-subtasks-info">
          <SubtasksIcon />
          <span>Subtasks {isDone ? '2/2' : '0/4'}</span>
        </div>
        <ChevronRightIcon className="task-subtasks-chevron" />
      </div>
    </div>
  )
}
