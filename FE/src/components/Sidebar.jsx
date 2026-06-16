import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Sidebar.css';

const ROLE_LABELS = {
  Customer: 'Khách hàng',
  SupportStaff: 'Nhân viên hỗ trợ',
  DepartmentManager: 'Quản lý phòng ban',
  SystemAdmin: 'Quản trị viên',
};

const navItems = [
  {
    label: 'Dashboard',
    to: '/dashboard',
    icon: '📊',
    roles: ['Customer', 'SupportStaff', 'DepartmentManager', 'SystemAdmin'],
  },
  {
    label: 'Gửi phản hồi',
    to: '/submit-feedback',
    icon: '✍️',
    roles: ['Customer'],
  },
  {
    label: 'Phản hồi của tôi',
    to: '/my-feedbacks',
    icon: '📋',
    roles: ['Customer'],
  },
  {
    label: 'Phản hồi được giao',
    to: '/assigned-feedbacks',
    icon: '📌',
    roles: ['SupportStaff', 'DepartmentManager', 'SystemAdmin'],
  },
  {
    label: 'Báo cáo',
    to: '/reports',
    icon: '📈',
    roles: ['DepartmentManager', 'SystemAdmin'],
  },
  {
    label: 'Quản lý người dùng',
    to: '/manage-users',
    icon: '👥',
    roles: ['SystemAdmin'],
  },
  {
    label: 'Nhật ký hệ thống',
    to: '/audit-logs',
    icon: '🔍',
    roles: ['SystemAdmin'],
  },
];

const Sidebar = ({ isOpen, onToggle }) => {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const visibleItems = navItems.filter((item) => hasRole(item.roles));

  return (
    <>
      <div className={`sidebar-overlay ${isOpen ? 'active' : ''}`} onClick={onToggle} />

      <aside className={`sidebar ${isOpen ? 'open' : 'collapsed'}`}>
        <div className="sidebar-logo">
          <div className="logo-icon">💬</div>
          {isOpen && (
            <div className="logo-text">
              <span className="logo-title">FeedbackHub</span>
              <span className="logo-sub">Management System</span>
            </div>
          )}
        </div>

        {isOpen && user && (
          <div className="sidebar-user">
            <div className="user-avatar">
              {user.fullName?.charAt(0)?.toUpperCase() || user.email?.charAt(0)?.toUpperCase() || '?'}
            </div>
            <div className="user-info">
              <p className="user-name truncate">{user.fullName || user.email}</p>
              <span className="role-badge">{ROLE_LABELS[user.role] || user.role}</span>
            </div>
          </div>
        )}

        <nav className="sidebar-nav">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `nav-item ${isActive ? 'active' : ''}`
              }
              title={!isOpen ? item.label : undefined}
            >
              <span className="nav-icon">{item.icon}</span>
              {isOpen && <span className="nav-label">{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <button className="logout-btn" onClick={handleLogout} title={!isOpen ? 'Đăng xuất' : undefined}>
            <span className="nav-icon">🚪</span>
            {isOpen && <span>Đăng xuất</span>}
          </button>
        </div>
      </aside>
    </>
  );
};

export default Sidebar;
