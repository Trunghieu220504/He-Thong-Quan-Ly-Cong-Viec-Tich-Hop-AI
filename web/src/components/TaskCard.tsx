import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { useQuery } from '@tanstack/react-query'
import { getTaskRisk } from '../api/risk.ts'
import type { TaskItem } from '../types/task.ts'

interface Props {
  task: TaskItem
  isViewer?: boolean
}

export function TaskCard({ task, isViewer = false }: Props) {
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
      return <span className="risk-badge risk-unknown">AI...</span>
    }

    if (!riskData || !riskData.risk) {
      return (
        <span className="risk-badge risk-unknown" title="Dịch vụ AI chưa khả dụng (503)">
          Chưa có dự đoán
        </span>
      )
    }

    const { risk } = riskData
    if (risk === 'low') {
      return <span className="risk-badge risk-low">AI: Rủi ro thấp</span>
    }
    if (risk === 'medium') {
      return <span className="risk-badge risk-medium">AI: Rủi ro vừa</span>
    }
    if (risk === 'high') {
      return <span className="risk-badge risk-high">AI: Rủi ro cao</span>
    }

    return (
      <span className="risk-badge risk-unknown">
        Chưa có dự đoán
      </span>
    )
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className={`task-card ${isDragging ? 'dragging' : ''}`}
      style-cursor={isViewer ? 'default' : 'grab'}
    >
      <div className="task-card-header">
        <h3 className="task-card-title">{task.title}</h3>
      </div>

      {task.description && (
        <p className="task-card-desc">{task.description}</p>
      )}

      <div className="task-card-footer">
        <div className="task-meta-left">
          <span className={`priority-badge priority-${task.priority}`}>
            {task.priority === 'Low' ? 'Thấp' : task.priority === 'Medium' ? 'Trung bình' : 'Khẩn cấp'}
          </span>
          {renderRiskBadge()}
        </div>

        {task.assigneeName && (
          <span className="task-assignee" title={`Người thực hiện: ${task.assigneeName}`}>
            👤 {task.assigneeName}
          </span>
        )}
      </div>
    </div>
  )
}
