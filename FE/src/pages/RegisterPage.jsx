import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './AuthPages.css';

<<<<<<< HEAD
const passwordPolicyMessage = 'Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.';

=======
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
const RegisterPage = () => {
  const { register, loading } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({
<<<<<<< HEAD
    firstName: '',
    lastName: '',
    phoneNumber: '',
=======
    fullName: '',
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    email: '',
    password: '',
    confirmPassword: '',
  });
<<<<<<< HEAD
  const [error, setError] = useState('');
=======
  const [error, setError]     = useState('');
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  const [success, setSuccess] = useState('');
  const [showPass, setShowPass] = useState(false);

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const validate = () => {
<<<<<<< HEAD
    if (!form.firstName.trim()) return 'Vui lòng nhập tên.';
    if (!form.lastName.trim()) return 'Vui lòng nhập họ.';
    if (!form.email.trim()) return 'Vui lòng nhập email.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) return 'Email không hợp lệ.';
    if (!/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/.test(form.password)) return passwordPolicyMessage;
=======
    if (!form.fullName.trim()) return 'Vui lòng nhập họ và tên.';
    if (!form.email.trim())    return 'Vui lòng nhập email.';
    if (form.password.length < 6) return 'Mật khẩu phải có ít nhất 6 ký tự.';
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    if (form.password !== form.confirmPassword) return 'Mật khẩu xác nhận không khớp.';
    return null;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const err = validate();
    if (err) { setError(err); return; }

<<<<<<< HEAD
    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      phoneNumber: form.phoneNumber.trim() || null,
      email: form.email.trim(),
      password: form.password,
      confirmPassword: form.confirmPassword,
    };

=======
    const { confirmPassword, ...payload } = form;
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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

<<<<<<< HEAD
        {error && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
=======
        {error   && <div className="alert alert-error"><span>⚠️</span> {error}</div>}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
        {success && <div className="alert alert-success"><span>✅</span> {success}</div>}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-grid">
            <div className="form-group">
<<<<<<< HEAD
              <label className="form-label" htmlFor="reg-lastname">Họ</label>
              <div className="input-wrap">
                <span className="input-icon">👤</span>
                <input
                  id="reg-lastname"
                  className="form-control with-icon"
                  type="text"
                  name="lastName"
                  placeholder="Nguyễn Văn"
                  value={form.lastName}
                  onChange={handleChange}
                  autoComplete="family-name"
                />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reg-firstname">Tên</label>
              <div className="input-wrap">
                <span className="input-icon">👤</span>
                <input
                  id="reg-firstname"
                  className="form-control with-icon"
                  type="text"
                  name="firstName"
                  placeholder="A"
                  value={form.firstName}
                  onChange={handleChange}
                  autoComplete="given-name"
=======
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
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
<<<<<<< HEAD
              <label className="form-label" htmlFor="reg-phone">Số điện thoại</label>
              <div className="input-wrap">
                <span className="input-icon">📞</span>
                <input
                  id="reg-phone"
                  className="form-control with-icon"
                  type="tel"
                  name="phoneNumber"
                  placeholder="0901234567"
                  value={form.phoneNumber}
                  onChange={handleChange}
                  autoComplete="tel"
                />
              </div>
            </div>

            <div className="form-group">
=======
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
              <label className="form-label" htmlFor="reg-password">Mật khẩu</label>
              <div className="input-wrap">
                <span className="input-icon">🔒</span>
                <input
                  id="reg-password"
                  className="form-control with-icon with-suffix"
                  type={showPass ? 'text' : 'password'}
                  name="password"
<<<<<<< HEAD
                  placeholder="Aa123456!"
=======
                  placeholder="Tối thiểu 6 ký tự"
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
                  value={form.password}
                  onChange={handleChange}
                  autoComplete="new-password"
                />
<<<<<<< HEAD
                <button type="button" className="input-suffix" onClick={() => setShowPass(!showPass)}>
=======
                <button
                  type="button"
                  className="input-suffix"
                  onClick={() => setShowPass(!showPass)}
                >
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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

<<<<<<< HEAD
          <p className="text-muted" style={{ marginTop: 8 }}>{passwordPolicyMessage}</p>

          <button id="register-submit-btn" type="submit" className="btn btn-primary btn-full btn-lg" disabled={loading} style={{ marginTop: 8 }}>
=======
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
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
