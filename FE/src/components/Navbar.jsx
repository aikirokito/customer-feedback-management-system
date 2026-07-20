import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import userApi from '../api/userApi';
import './Navbar.css';

const ROLE_LABELS = {
  Customer: 'Khách hàng',
  SupportStaff: 'Nhân viên hỗ trợ',
  DepartmentManager: 'Quản lý',
  SystemAdmin: 'Quản trị viên',
};

const Navbar = ({ onMenuToggle }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    let active = true;
    const loadUnreadCount = () => userApi.getUnreadCount()
      .then((response) => { if (active) setUnreadCount(response.data?.unreadCount || 0); })
      .catch(() => {});
    void loadUnreadCount();
    const intervalId = window.setInterval(loadUnreadCount, 60000);
    return () => { active = false; window.clearInterval(intervalId); };
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="navbar">
      <div className="navbar-left">
        <button
          id="menu-toggle-btn"
          className="menu-toggle"
          onClick={onMenuToggle}
          aria-label="Toggle menu"
        >
          <span className="hamburger" />
          <span className="hamburger" />
          <span className="hamburger" />
        </button>
        <div className="navbar-breadcrumb">
          <span className="brand-text">FeedbackHub</span>
        </div>
      </div>

      <div className="navbar-right">
        <Link className="notification-link" to="/notifications" aria-label={`${unreadCount} thông báo chưa đọc`}>
          <span aria-hidden="true">🔔</span>
          {unreadCount > 0 && <span className="notification-count">{unreadCount > 99 ? '99+' : unreadCount}</span>}
        </Link>
        <div className="nav-user">
          <div className="nav-user-info">
            <span className="nav-user-name">{user?.fullName || user?.email}</span>
            <span className="nav-user-role">{ROLE_LABELS[user?.role] || user?.role}</span>
          </div>
          <div className="nav-avatar">
            {user?.fullName?.charAt(0)?.toUpperCase() || user?.email?.charAt(0)?.toUpperCase() || '?'}
          </div>
          <div className="nav-dropdown">
            <Link className="dropdown-item" to="/profile">👤 Hồ sơ cá nhân</Link>
            <Link className="dropdown-item" to="/notifications">🔔 Thông báo</Link>
            <button
              id="navbar-logout-btn"
              className="dropdown-item danger"
              onClick={handleLogout}
            >
              🚪 Đăng xuất
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Navbar;
