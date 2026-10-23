import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Navbar } from './components/Navbar.tsx'
import { ProtectedRoute } from './components/ProtectedRoute.tsx'
import { LoginPage } from './pages/LoginPage.tsx'
import { ProjectBoardPage } from './pages/ProjectBoardPage.tsx'
import { ProjectsPage } from './pages/ProjectsPage.tsx'

export function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        {/* Các route yêu cầu đăng nhập */}
        <Route element={<ProtectedRoute />}>
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/projects/:projectId" element={<ProjectBoardPage />} />
        </Route>

        {/* Mặc định chuyển về /projects */}
        <Route path="/" element={<Navigate to="/projects" replace />} />
        <Route path="*" element={<Navigate to="/projects" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
