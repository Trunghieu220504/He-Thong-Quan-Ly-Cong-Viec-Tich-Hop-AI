import { useDroppable } from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { TaskCard } from './TaskCard.tsx'
import type { TaskItem, TaskStatus } from '../types/task.ts'

interface Props {
  status: TaskStatus
  title: string
  tasks: TaskItem[]
  isViewer?: boolean
}

export function KanbanColumn({ status, title, tasks, isViewer = false }: Props) {
  const { setNodeRef, isOver } = useDroppable({
    id: status,
    data: {
      type: 'Column',
      status,
    },
  })

  return (
    <div
      ref={setNodeRef}
      className="kanban-column"
      style={{
        borderColor: isOver ? 'var(--primary)' : undefined,
        backgroundColor: isOver ? '#eef2ff' : undefined,
      }}
    >
      <div className="column-header">
        <div className="column-title-group">
          <span className={`column-dot ${status}`} />
          <h2 className="column-title">{title}</h2>
        </div>
        <span className="task-count">{tasks.length}</span>
      </div>

      <div className="column-tasks-container">
        <SortableContext items={tasks.map((t) => t.id)} strategy={verticalListSortingStrategy}>
          {tasks.map((task) => (
            <TaskCard key={task.id} task={task} isViewer={isViewer} />
          ))}
        </SortableContext>

        {tasks.length === 0 && (
          <div style={{ textAlign: 'center', padding: '30px 10px', color: 'var(--text-light)', fontSize: '0.85rem' }}>
            Kéo công việc vào đây
          </div>
        )}
      </div>
    </div>
  )
}
