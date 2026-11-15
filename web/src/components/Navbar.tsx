import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { IS_MOCK } from '../api/client.ts'
import { useAuthStore } from '../stores/authStore.ts'
import {
  AppLogoIcon,
  AppSwitcherIcon,
  PlusIcon,
  QuestionIcon,
  SearchIcon,
  SettingsIcon,
} from './AppIcons.tsx'
import { NotificationBell } from './NotificationBell.tsx'

interface Props {
  onOpenCreateTask?: () => void
}

export function Navbar({ onOpenCreateTask }: Props) {
  const { user, isAuthenticated, logout } = useAuthStore()
  const navigate = useNavigate()
  const [showProfileMenu, setShowProfileMenu] = useState(false)

  const handleLogout = () => {
    logout()
    setShowProfileMenu(false)
    navigate('/login')
  }

  const handleCreateClick = () => {
    if (onOpenCreateTask) {
      onOpenCreateTask()
    } else {
      window.dispatchEvent(new CustomEvent('pmai-open-create-task'))
    }
  }

  const userInitials = user?.displayName
    ? user.displayName
        .split(' ')
        .map((w) => w[0])
        .join('')
        .slice(0, 2)
        .toUpperCase()
    : 'BA'

  return (
    <header className="global-app-header">
      <div className="header-brand-section">
        <button type="button" className="header-icon-btn" title="Ứng dụng">
          <AppSwitcherIcon />
        </button>

        <Link to={isAuthenticated ? '/projects' : '/login'} className="brand-logo-link">
          <AppLogoIcon />
          <span className="brand-title">PMAI <span className="brand-subtitle">Workspace</span></span>
        </Link>

        {IS_MOCK && (
          <span className="mock-mode-pill" title="Đang chạy ở chế độ Mock Data">
            Mock Mode
          </span>
        )}
      </div>

      <div className="header-search-section">
        <div className="search-input-box">
          <SearchIcon className="search-icon-inside" />
          <input
            type="text"
            className="global-search-input"
            placeholder="Search"
            aria-label="Tìm kiếm trong PMAI"
          />
        </div>
      </div>

      <div className="header-actions-section">
        {isAuthenticated && (
          <button
            type="button"
            onClick={handleCreateClick}
            className="action-create-btn"
            title="Tạo công việc mới"
          >
            <PlusIcon />
            <span>Create</span>
          </button>
        )}

        <div className="status-badge-pill" title="Hệ thống PMAI tích hợp AI">
          <span className="badge-icon">⚡</span>
          <span>PMAI Agile</span>
        </div>

        <NotificationBell />

        <button type="button" className="header-icon-btn" title="Trợ giúp">
          <QuestionIcon />
        </button>

        <button type="button" className="header-icon-btn" title="Cài đặt">
          <SettingsIcon />
        </button>

        {isAuthenticated && user ? (
          <div className="profile-menu-container">
            <button
              type="button"
              onClick={() => setShowProfileMenu((prev) => !prev)}
              className="user-avatar-btn"
              title={`${user.displayName || user.email} (Nhấn để xem tùy chọn)`}
            >
              <div className="user-avatar-circle">{userInitials}</div>
            </button>

            {showProfileMenu && (
              <div className="profile-dropdown-card">
                <div className="profile-info-block">
                  <div className="profile-display-name">{user.displayName || 'Người dùng'}</div>
                  <div className="profile-email-text">{user.email}</div>
                </div>
                <div className="profile-menu-separator" />
                <button
                  type="button"
                  onClick={handleLogout}
                  className="profile-menu-action logout"
                >
                  Đăng xuất
                </button>
              </div>
            )}
          </div>
        ) : (
          <Link to="/login" className="action-create-btn" style={{ textDecoration: 'none' }}>
            Đăng nhập
          </Link>
        )}
      </div>
    </header>
  )
}
