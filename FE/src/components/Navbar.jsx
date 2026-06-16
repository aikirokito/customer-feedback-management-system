import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Navbar.css';

const ROLE_LABELS = {
  CUSTOMER: 'Khách hàng',
  SUPPORT_STAFF: 'Nhân viên hỗ trợ',
  MANAGER: 'Quản lý',
  ADMIN: 'Quản trị viên',
};

const Navbar = ({ onMenuToggle }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

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
        <div className="nav-user">
          <div className="nav-user-info">
            <span className="nav-user-name">{user?.fullName || user?.email}</span>
            <span className="nav-user-role">{ROLE_LABELS[user?.role] || user?.role}</span>
          </div>
          <div className="nav-avatar">
            {user?.fullName?.charAt(0)?.toUpperCase() || user?.email?.charAt(0)?.toUpperCase() || '?'}
          </div>
          <div className="nav-dropdown">
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
