import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import authApi from '../api/authApi';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';

const ProfilePage = () => {
  const { user, updateCurrentUser, logout } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    phoneNumber: user?.phoneNumber || '',
  });
  const [password, setPassword] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [savingProfile, setSavingProfile] = useState(false);
  const [savingPassword, setSavingPassword] = useState(false);
  const [message, setMessage] = useState({ text: '', type: '' });

  const saveProfile = async (event) => {
    event.preventDefault();
    setSavingProfile(true);
    setMessage({ text: '', type: '' });
    try {
      const response = await userApi.updateMyProfile({
        firstName: profile.firstName.trim(),
        lastName: profile.lastName.trim(),
        phoneNumber: profile.phoneNumber.trim() || null,
      });
      updateCurrentUser(response.data);
      setMessage({ text: 'Hồ sơ đã được cập nhật.', type: 'success' });
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể cập nhật hồ sơ.', type: 'error' });
    } finally {
      setSavingProfile(false);
    }
  };

  const changePassword = async (event) => {
    event.preventDefault();
    if (password.newPassword !== password.confirmNewPassword) {
      setMessage({ text: 'Mật khẩu xác nhận không khớp.', type: 'error' });
      return;
    }

    setSavingPassword(true);
    setMessage({ text: '', type: '' });
    try {
      await authApi.changePassword(password);
      await logout();
      navigate('/login', { replace: true, state: { passwordChanged: true } });
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể đổi mật khẩu.', type: 'error' });
      setSavingPassword(false);
    }
  };

  return (
    <div>
      <div className="page-header">
        <h1>Hồ sơ cá nhân</h1>
        <p>Cập nhật thông tin liên hệ và bảo mật tài khoản.</p>
      </div>

      {message.text && <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>{message.text}</div>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.25rem' }}>
        <form className="card" onSubmit={saveProfile}>
          <div className="card-header"><h2 className="card-title">Thông tin tài khoản</h2></div>
          <div className="form-group">
            <label className="form-label">Email</label>
            <input className="form-control" value={user?.email || ''} disabled />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="profile-first-name">Tên</label>
            <input id="profile-first-name" className="form-control" maxLength={100} required value={profile.firstName} onChange={(e) => setProfile({ ...profile, firstName: e.target.value })} />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="profile-last-name">Họ</label>
            <input id="profile-last-name" className="form-control" maxLength={100} required value={profile.lastName} onChange={(e) => setProfile({ ...profile, lastName: e.target.value })} />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="profile-phone">Số điện thoại</label>
            <input id="profile-phone" className="form-control" maxLength={20} value={profile.phoneNumber} onChange={(e) => setProfile({ ...profile, phoneNumber: e.target.value })} />
          </div>
          <button className="btn btn-primary" disabled={savingProfile}>{savingProfile ? 'Đang lưu...' : 'Lưu hồ sơ'}</button>
        </form>

        <form className="card" onSubmit={changePassword}>
          <div className="card-header"><h2 className="card-title">Đổi mật khẩu</h2></div>
          <div className="form-group">
            <label className="form-label" htmlFor="current-password">Mật khẩu hiện tại</label>
            <input id="current-password" className="form-control" type="password" autoComplete="current-password" required value={password.currentPassword} onChange={(e) => setPassword({ ...password, currentPassword: e.target.value })} />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="new-password">Mật khẩu mới</label>
            <input id="new-password" className="form-control" type="password" autoComplete="new-password" required value={password.newPassword} onChange={(e) => setPassword({ ...password, newPassword: e.target.value })} />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="confirm-password">Xác nhận mật khẩu mới</label>
            <input id="confirm-password" className="form-control" type="password" autoComplete="new-password" required value={password.confirmNewPassword} onChange={(e) => setPassword({ ...password, confirmNewPassword: e.target.value })} />
          </div>
          <p className="text-muted mb-4">Sau khi đổi mật khẩu, tất cả phiên đăng nhập sẽ bị thu hồi.</p>
          <button className="btn btn-secondary" disabled={savingPassword}>{savingPassword ? 'Đang đổi...' : 'Đổi mật khẩu'}</button>
        </form>
      </div>
    </div>
  );
};

export default ProfilePage;
