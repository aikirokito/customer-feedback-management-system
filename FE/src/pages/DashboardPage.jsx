import { useAuth } from '../context/AuthContext';

const DashboardPage = () => {
  const { user } = useAuth();

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1>Dashboard</h1>
        <p>Xin chào, {user?.fullName || user?.email}! Chào mừng bạn đến với hệ thống quản lý phản hồi.</p>
      </div>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--primary-glow)', color: 'var(--primary-light)' }}>
            📊
          </div>
          <div className="stat-info">
            <div className="stat-value">--</div>
            <div className="stat-label">Tổng số phản hồi</div>
          </div>
        </div>
        
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--warning-light)', color: 'var(--warning)' }}>
            ⏳
          </div>
          <div className="stat-info">
            <div className="stat-value">--</div>
            <div className="stat-label">Đang chờ xử lý</div>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--success-light)', color: 'var(--success)' }}>
            ✅
          </div>
          <div className="stat-info">
            <div className="stat-value">--</div>
            <div className="stat-label">Đã giải quyết</div>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Hoạt động gần đây</h2>
        </div>
        <div className="empty-state">
          <div className="empty-icon">📭</div>
          <h3>Chưa có dữ liệu</h3>
          <p>Dữ liệu hoạt động sẽ hiển thị ở đây.</p>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
