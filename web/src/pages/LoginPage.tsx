import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { login, register } from '../api/auth.ts'
import { ApiError } from '../api/client.ts'
import { useAuthStore } from '../stores/authStore.ts'

export function LoginPage() {
  const { isAuthenticated, setAuth } = useAuthStore()
  const navigate = useNavigate()

  const [isRegisterTab, setIsRegisterTab] = useState(false)
  const [email, setEmail] = useState('baoanh@example.com')
  const [password, setPassword] = useState('Password123!')
  const [displayName, setDisplayName] = useState('Bảo Anh')

  const [loading, setLoading] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/projects" replace />
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErrorMsg(null)
    setSuccessMsg(null)
    setLoading(true)

    try {
      if (isRegisterTab) {
        if (password.length < 8) {
          setErrorMsg('Mật khẩu phải có ít nhất 8 ký tự.')
          setLoading(false)
          return
        }
        await register({ email, password, displayName })
        setSuccessMsg('Đăng ký tài khoản thành công! Đang tự động đăng nhập...')
        // Tự động login sau khi đăng ký
        const tokens = await login({ email, password })
        setAuth(tokens, { id: '', email, displayName })
        navigate('/projects')
      } else {
        const tokens = await login({ email, password })
        setAuth(tokens)
        navigate('/projects')
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setErrorMsg(err.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.')
      } else if (err instanceof Error) {
        setErrorMsg(err.message)
      } else {
        setErrorMsg('Có lỗi xảy ra, vui lòng thử lại sau.')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleDemoLogin = () => {
    // Đăng nhập nhanh bằng dữ liệu mẫu
    const demo = {
      tokens: {
        accessToken: `demo-token-${Date.now()}`,
        accessTokenExpiresAtUtc: new Date(Date.now() + 3600000).toISOString(),
        refreshToken: `demo-refresh-${Date.now()}`,
        refreshTokenExpiresAtUtc: new Date(Date.now() + 86400000).toISOString(),
      },
      user: {
        id: 'u-user-01',
        email: 'baoanh@example.com',
        displayName: 'Bảo Anh (Bạn)',
      },
    }
    setAuth(demo.tokens, demo.user)
    navigate('/projects')
  }

  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <div className="auth-header">
          <h1>{isRegisterTab ? 'Tạo tài khoản' : 'Đăng nhập'}</h1>
          <p className="subtitle">
            {isRegisterTab
              ? 'Tạo tài khoản thành viên để tham gia dự án'
              : 'Hệ Thống Quản Lý Công Việc Tích Hợp AI'}
          </p>
        </div>

        <div className="auth-tabs">
          <button
            type="button"
            className={`auth-tab ${!isRegisterTab ? 'active' : ''}`}
            onClick={() => {
              setIsRegisterTab(false)
              setErrorMsg(null)
            }}
          >
            Đăng nhập
          </button>
          <button
            type="button"
            className={`auth-tab ${isRegisterTab ? 'active' : ''}`}
            onClick={() => {
              setIsRegisterTab(true)
              setErrorMsg(null)
            }}
          >
            Đăng ký
          </button>
        </div>

        {errorMsg && (
          <div className="alert alert-error">
            <span>{errorMsg}</span>
          </div>
        )}

        {successMsg && (
          <div className="alert alert-info">
            <span>{successMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {isRegisterTab && (
            <div className="form-group">
              <label className="form-label" htmlFor="displayName">
                Họ và tên
              </label>
              <input
                id="displayName"
                type="text"
                className="form-input"
                placeholder="Nguyễn Trọng Bảo Anh"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                required
                minLength={2}
                maxLength={100}
              />
            </div>
          )}

          <div className="form-group">
            <label className="form-label" htmlFor="email">
              Địa chỉ Email
            </label>
            <input
              id="email"
              type="email"
              className="form-input"
              placeholder="ten@congty.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              maxLength={254}
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Mật khẩu
            </label>
            <input
              id="password"
              type="password"
              className="form-input"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
              maxLength={72}
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', marginTop: '12px' }}
            disabled={loading}
          >
            {loading ? 'Đang xử lý...' : isRegisterTab ? 'Đăng ký tài khoản' : 'Đăng nhập vào hệ thống'}
          </button>

          <div style={{ marginTop: '16px', textAlign: 'center', borderTop: '1px solid var(--border)', paddingTop: '16px' }}>
            <button
              type="button"
              onClick={handleDemoLogin}
              className="btn btn-secondary btn-sm"
              style={{ width: '100%' }}
              title="Đăng nhập ngay để xem trước giao diện khi backend chưa bật"
            >
              🚀 Vào nhanh bằng chế độ Demo (Mock Data)
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
