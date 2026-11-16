import { useState } from 'react'
import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AppSidebar } from './components/AppSidebar.tsx'
import { CreateProjectModal } from './components/CreateProjectModal.tsx'
import { Navbar } from './components/Navbar.tsx'
import { ProtectedRoute } from './components/ProtectedRoute.tsx'
import { ToastContainer } from './components/ToastContainer.tsx'
import { LoginPage } from './pages/LoginPage.tsx'
import { ProjectBoardPage } from './pages/ProjectBoardPage.tsx'
import { ProjectsPage } from './pages/ProjectsPage.tsx'
import './App.css'

function MainLayout() {
  const [isCreateProjOpen, setIsCreateProjOpen] = useState(false)

  return (
    <div className="app-workspace-body">
      <AppSidebar onOpenCreateProject={() => setIsCreateProjOpen(true)} />
      <main className="app-main-content">
        <Outlet />
      </main>
      <CreateProjectModal
        isOpen={isCreateProjOpen}
        onClose={() => setIsCreateProjOpen(false)}
      />
    </div>
  )
}

export function App() {
  return (
    <BrowserRouter>
      <div className="app-root-shell">
        <Navbar />
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          {/* Các route yêu cầu đăng nhập bọc trong Sidebar Layout */}
          <Route element={<ProtectedRoute />}>
            <Route element={<MainLayout />}>
              <Route path="/projects" element={<ProjectsPage />} />
              <Route path="/projects/:projectId" element={<ProjectBoardPage />} />
            </Route>
          </Route>

          {/* Mặc định chuyển về /projects */}
          <Route path="/" element={<Navigate to="/projects" replace />} />
          <Route path="*" element={<Navigate to="/projects" replace />} />
        </Routes>
        <ToastContainer />
      </div>
    </BrowserRouter>
  )
}

export default App
