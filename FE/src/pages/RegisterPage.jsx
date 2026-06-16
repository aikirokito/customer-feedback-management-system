import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './AuthPages.css';

const RegisterPage = () => {
  const { register, loading } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [error, setError]     = useState('');
  const [success, setSuccess] = useState('');
  const [showPass, setShowPass] = useState(false);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const validate = () => {
    if (!form.fullName.trim()) return 'Vui lòng nhập họ và tên.';
    if (!form.email.trim())    return 'Vui lòng nhập email.';
    if (form.password.length < 6) return 'Mật khẩu phải có ít nhất 6 ký tự.';
    if (form.password !== form.confirmPassword) return 'Mật khẩu xác nhận không khớp.';
    return null;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const err = validate();
    if (err) { setError(err); return; }

    const { confirmPassword, ...payload } = form;
    const result = await register(payload);
    if (result.success) {
      setSuccess('Đăng ký thành công! Đang chuyển hướng...');
      setTimeout(() => navigate('/login'), 1800);
    } else {
      setError(result.message);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-glow glow-1" />
      <div className="auth-glow glow-2" />

      <div className="auth-card auth-card-wide">
        <div className="auth-header">
          <div className="auth-logo">🌟</div>
          <h1 className="auth-title">Tạo tài khoản</h1>
          <p className="auth-subtitle">Điền thông tin để đăng ký tài khoản mới</p>
        </div>

        {error   && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
        {success && <div className="alert alert-success"><span>✅</span> {success}</div>}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-grid">
            <div className="form-group">
              <label className="form-label" htmlFor="reg-fullname">Họ và tên</label>
              <div className="input-wrap">
                <span className="input-icon">👤</span>
                <input
                  id="reg-fullname"
                  className="form-control with-icon"
                  type="text"
                  name="fullName"
                  placeholder="Nguyễn Văn A"
                  value={form.fullName}
                  onChange={handleChange}
                  autoComplete="name"
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-email">Email</label>
              <div className="input-wrap">
                <span className="input-icon">✉️</span>
                <input
                  id="reg-email"
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
              <label className="form-label" htmlFor="reg-password">Mật khẩu</label>
              <div className="input-wrap">
                <span className="input-icon">🔒</span>
                <input
                  id="reg-password"
                  className="form-control with-icon with-suffix"
                  type={showPass ? 'text' : 'password'}
                  name="password"
                  placeholder="Tối thiểu 6 ký tự"
                  value={form.password}
                  onChange={handleChange}
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  className="input-suffix"
                  onClick={() => setShowPass(!showPass)}
                >
                  {showPass ? '🙈' : '👁️'}
                </button>
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-confirm">Xác nhận mật khẩu</label>
              <div className="input-wrap">
                <span className="input-icon">🔐</span>
                <input
                  id="reg-confirm"
                  className="form-control with-icon"
                  type={showPass ? 'text' : 'password'}
                  name="confirmPassword"
                  placeholder="Nhập lại mật khẩu"
                  value={form.confirmPassword}
                  onChange={handleChange}
                  autoComplete="new-password"
                />
              </div>
            </div>
          </div>

          <div className="password-strength">
            <div className="strength-bar">
              <div
                className="strength-fill"
                style={{ width: `${Math.min((form.password.length / 12) * 100, 100)}%`,
                  background: form.password.length < 6 ? '#ef4444'
                    : form.password.length < 10 ? '#f59e0b' : '#10b981'
                }}
              />
            </div>
            <span className="strength-text text-muted">
              {form.password.length === 0 ? '' : form.password.length < 6 ? 'Yếu' : form.password.length < 10 ? 'Trung bình' : 'Mạnh'}
            </span>
          </div>

          <button
            id="register-submit-btn"
            type="submit"
            className="btn btn-primary btn-full btn-lg"
            disabled={loading}
            style={{ marginTop: 8 }}
          >
            {loading ? <><span className="btn-spinner" /> Đang đăng ký...</> : '✨ Tạo tài khoản'}
          </button>
        </form>

        <div className="auth-divider">
          <span>Đã có tài khoản?</span>
        </div>

        <Link to="/login" id="go-login-link" className="btn btn-secondary btn-full">
          🚀 Đăng nhập ngay
        </Link>
      </div>
    </div>
  );
};

export default RegisterPage;
