import { Link, useNavigate } from 'react-router-dom'
import { IS_MOCK } from '../api/client.ts'
import { useAuthStore } from '../stores/authStore.ts'

export function Navbar() {
  const { user, isAuthenticated, logout } = useAuthStore()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <header className="app-header">
      <Link to={isAuthenticated ? '/projects' : '/login'} className="brand-logo">
        <span className="brand-badge">PMAI</span>
        <span>Quản Lý Công Việc AI</span>
      </Link>

      <div className="header-actions">
        {IS_MOCK && (
          <span className="priority-badge priority-Medium" title="Đang chạy ở chế độ Mock Data">
            Mock Mode
          </span>
        )}

        {isAuthenticated && user && (
          <>
            <div className="user-tag">
              <div className="avatar">
                {user.displayName ? user.displayName.charAt(0).toUpperCase() : 'U'}
              </div>
              <span>{user.displayName || user.email}</span>
            </div>
            <button type="button" onClick={handleLogout} className="btn btn-secondary btn-sm">
              Đăng xuất
            </button>
          </>
        )}
      </div>
    </header>
  )
}
