import { NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Sidebar.css';

const ROLE_LABELS = {
<<<<<<< HEAD
  Customer: 'Khách hàng',
  SupportStaff: 'Nhân viên hỗ trợ',
  DepartmentManager: 'Quản lý phòng ban',
  SystemAdmin: 'Quản trị viên',
=======
  CUSTOMER: 'Khách hàng',
  SUPPORT_STAFF: 'Nhân viên hỗ trợ',
  MANAGER: 'Quản lý phòng ban',
  ADMIN: 'Quản trị viên',
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
};

const navItems = [
  {
    label: 'Dashboard',
    to: '/dashboard',
    icon: '📊',
<<<<<<< HEAD
    roles: ['Customer', 'SupportStaff', 'DepartmentManager', 'SystemAdmin'],
=======
    roles: ['CUSTOMER', 'SUPPORT_STAFF', 'MANAGER', 'ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Gửi phản hồi',
    to: '/submit-feedback',
    icon: '✍️',
<<<<<<< HEAD
    roles: ['Customer'],
=======
    roles: ['CUSTOMER', 'ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Phản hồi của tôi',
    to: '/my-feedbacks',
    icon: '📋',
<<<<<<< HEAD
    roles: ['Customer'],
=======
    roles: ['CUSTOMER', 'ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Phản hồi được giao',
    to: '/assigned-feedbacks',
    icon: '📌',
<<<<<<< HEAD
    roles: ['SupportStaff', 'DepartmentManager', 'SystemAdmin'],
=======
    roles: ['SUPPORT_STAFF', 'MANAGER', 'ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Báo cáo',
    to: '/reports',
    icon: '📈',
<<<<<<< HEAD
    roles: ['DepartmentManager', 'SystemAdmin'],
=======
    roles: ['MANAGER', 'ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Quản lý người dùng',
    to: '/manage-users',
    icon: '👥',
<<<<<<< HEAD
    roles: ['SystemAdmin'],
=======
    roles: ['ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  },
  {
    label: 'Nhật ký hệ thống',
    to: '/audit-logs',
    icon: '🔍',
<<<<<<< HEAD
    roles: ['SystemAdmin'],
=======
    roles: ['ADMIN'],
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
<<<<<<< HEAD
      <div className={`sidebar-overlay ${isOpen ? 'active' : ''}`} onClick={onToggle} />

      <aside className={`sidebar ${isOpen ? 'open' : 'collapsed'}`}>
=======
      {/* Mobile overlay */}
      <div className={`sidebar-overlay ${isOpen ? 'active' : ''}`} onClick={onToggle} />

      <aside className={`sidebar ${isOpen ? 'open' : 'collapsed'}`}>
        {/* Logo */}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
        <div className="sidebar-logo">
          <div className="logo-icon">💬</div>
          {isOpen && (
            <div className="logo-text">
              <span className="logo-title">FeedbackHub</span>
              <span className="logo-sub">Management System</span>
            </div>
          )}
        </div>

<<<<<<< HEAD
=======
        {/* User info */}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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

<<<<<<< HEAD
=======
        {/* Nav */}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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

<<<<<<< HEAD
=======
        {/* Logout */}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
