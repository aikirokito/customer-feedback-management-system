import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth, getDefaultRouteForRole } from '../context/AuthContext';
import './AuthPages.css';

const PUBLIC_REDIRECTS = ['/login', '/register', '/unauthorized'];

const LoginPage = () => {
  const { login, googleLogin, loading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = location.state?.from?.pathname;

  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [showPass, setShowPass] = useState(false);
  const googleButtonRef = useRef(null);
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

  useEffect(() => {
    if (!googleClientId || !googleButtonRef.current) return undefined;
    let active = true;

    const initializeGoogle = () => {
      if (!active || !window.google?.accounts?.id || !googleButtonRef.current) return;
      window.google.accounts.id.initialize({
        client_id: googleClientId,
        callback: async ({ credential }) => {
          const result = await googleLogin(credential);
          if (result.success) {
            const safeFrom = from && !PUBLIC_REDIRECTS.includes(from) ? from : null;
            navigate(safeFrom || result.redirectTo || getDefaultRouteForRole(result.user), { replace: true });
          } else {
            setError(result.message);
          }
        },
      });
      googleButtonRef.current.innerHTML = '';
      window.google.accounts.id.renderButton(googleButtonRef.current, {
        theme: 'filled_black',
        size: 'large',
        width: 320,
        text: 'continue_with',
        locale: 'vi',
      });
    };

    let script = document.querySelector('script[data-google-identity]');
    if (!script) {
      script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client?hl=vi';
      script.async = true;
      script.dataset.googleIdentity = 'true';
      document.head.appendChild(script);
    }

    if (window.google?.accounts?.id) initializeGoogle();
    else script.addEventListener('load', initializeGoogle);
    return () => {
      active = false;
      script.removeEventListener('load', initializeGoogle);
    };
  }, [from, googleClientId, googleLogin, navigate]);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.email || !form.password) {
      setError('Vui lòng điền đầy đủ thông tin.');
      return;
    }
    const result = await login(form);
    if (result.success) {
      const safeFrom = from && !PUBLIC_REDIRECTS.includes(from) ? from : null;
      navigate(safeFrom || result.redirectTo || getDefaultRouteForRole(result.user), { replace: true });
    } else {
      setError(result.message);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-glow glow-1" />
      <div className="auth-glow glow-2" />

      <div className="auth-card">
        {/* Header */}
        <div className="auth-header">
          <div className="auth-logo">💬</div>
          <h1 className="auth-title">Chào mừng trở lại</h1>
          <p className="auth-subtitle">Đăng nhập vào hệ thống quản lý phản hồi</p>
        </div>

        {/* Error */}
        {error && (
          <div className="alert alert-error" role="alert">
            <span>⚠️</span> {error}
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label className="form-label" htmlFor="login-email">Email</label>
            <div className="input-wrap">
              <span className="input-icon">✉️</span>
              <input
                id="login-email"
                className="form-control with-icon"
                type="email"
                name="email"
                placeholder="example@email.com"
                value={form.email}
                onChange={handleChange}
                autoComplete="email"
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="login-password">Mật khẩu</label>
            <div className="input-wrap">
              <span className="input-icon">🔒</span>
              <input
                id="login-password"
                className="form-control with-icon with-suffix"
                type={showPass ? 'text' : 'password'}
                name="password"
                placeholder="••••••••"
                value={form.password}
                onChange={handleChange}
                autoComplete="current-password"
              />
              <button
                type="button"
                className="input-suffix"
                onClick={() => setShowPass(!showPass)}
                aria-label="Toggle password visibility"
              >
                {showPass ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          <button
            id="login-submit-btn"
            type="submit"
            className="btn btn-primary btn-full btn-lg"
            disabled={loading}
          >
            {loading ? (
              <><span className="btn-spinner" /> Đang đăng nhập...</>
            ) : (
              '🚀 Đăng nhập'
            )}
          </button>
        </form>

        {googleClientId && (
          <>
            <div className="auth-divider"><span>Hoặc</span></div>
            <div ref={googleButtonRef} style={{ display: 'flex', justifyContent: 'center', minHeight: 44 }} />
          </>
        )}

        <div className="auth-divider">
          <span>Chưa có tài khoản?</span>
        </div>

        <Link to="/register" id="go-register-link" className="btn btn-secondary btn-full">
          ✨ Tạo tài khoản mới
        </Link>
      </div>
    </div>
  );
};

export default LoginPage;
