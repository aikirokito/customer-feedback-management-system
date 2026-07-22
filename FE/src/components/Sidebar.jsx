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
    roles: ['Customer', 'SupportStaff', 'DepartmentManager'],
  },
  {
    label: 'Thông báo',
    to: '/notifications',
    icon: '🔔',
    roles: ['Customer', 'SupportStaff', 'DepartmentManager'],
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
    roles: ['SupportStaff', 'DepartmentManager'],
  },
  {
    label: 'Báo cáo',
    to: '/reports',
    icon: '📈',
    roles: ['DepartmentManager'],
  },
  {
    label: 'Quản lý người dùng',
    to: '/manage-users',
    icon: '👥',
    roles: ['SystemAdmin'],
  },
  {
    label: 'Quản lý danh mục',
    to: '/manage-categories',
    icon: '🗂️',
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
      {/* Mobile overlay */}
      <div className={`sidebar-overlay ${isOpen ? 'active' : ''}`} onClick={onToggle} />

      <aside className={`sidebar ${isOpen ? 'open' : 'collapsed'}`}>
        {/* Logo */}
        <div className="sidebar-logo">
          <div className="logo-icon">💬</div>
          {isOpen && (
            <div className="logo-text">
              <span className="logo-title">FeedbackHub</span>
              <span className="logo-sub">Management System</span>
            </div>
          )}
        </div>

        {/* User info */}
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

        {/* Nav */}
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

        {/* Logout */}
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
