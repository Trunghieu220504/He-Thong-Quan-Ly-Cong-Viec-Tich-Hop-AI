import { DndContext } from '@dnd-kit/core'
import { useCounterStore } from './stores/counterStore'

function App() {
  const { count, increment } = useCounterStore()

  return (
    <DndContext>
      <main>
        <h1>Project Management AI</h1>
        <p>Khung web đã chạy (chưa gọi API).</p>
        <button type="button" onClick={increment}>
          Đã bấm {count} lần
        </button>
      </main>
    </DndContext>
  )
}

export default App
