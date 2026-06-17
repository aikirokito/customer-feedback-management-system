import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import feedbackApi from '../api/feedbackApi';
import userApi from '../api/userApi';

const priorityBadge = (priority) => {
  if (priority === 'Urgent' || priority === 'High') return 'danger';
  if (priority === 'Medium') return 'warning';
  return 'info';
};

const statusBadge = (status) => {
  switch (status) {
    case 'New': return 'info';
    case 'Assigned': return 'primary';
    case 'InProgress': return 'warning';
    case 'Resolved': return 'success';
    case 'Closed': return 'gray';
    case 'Rejected': return 'danger';
    default: return 'primary';
  }
};

const DashboardPage = () => {
  const { user, hasRole } = useAuth();
  const isAdminOrManager = hasRole(['SystemAdmin', 'DepartmentManager']);
  const isStaffOnly = hasRole(['SupportStaff']) && !hasRole(['SystemAdmin', 'DepartmentManager']);
  const isCustomer = hasRole(['Customer']);

  const [stats, setStats] = useState({ total: 0, pending: 0, resolved: 0, avgTime: null });
  const [recentFeedbacks, setRecentFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const fetchDashboardData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      if (isAdminOrManager) {
        // Admin or Manager: Load actual reports summary and all recent feedbacks
        const [repRes, feedRes] = await Promise.all([
          userApi.getReports().catch(() => ({ data: {} })),
          feedbackApi.getAssignedFeedbacks({ pageSize: 5 }).catch(() => ({ data: [] }))
        ]);
        
        const repData = repRes.data || {};
        setStats({
          total: repData.totalFeedbacks || 0,
          pending: repData.openFeedbacks || 0,
          resolved: (repData.resolvedFeedbacks || 0) + (repData.closedFeedbacks || 0),
          avgTime: repData.averageResolutionTimeHours || null,
        });
        setRecentFeedbacks(feedRes.data || []);
      } 
      else if (isStaffOnly) {
        // Support Staff: Load assigned feedbacks and calculate stats client-side
        const [allFeedRes, recentRes] = await Promise.all([
          feedbackApi.getAssignedFeedbacks({ pageSize: 100 }).catch(() => ({ data: [] })),
          feedbackApi.getAssignedFeedbacks({ pageSize: 5 }).catch(() => ({ data: [] }))
        ]);

        const allItems = allFeedRes.data || [];
        const total = allItems.length;
        const pending = allItems.filter(f => !['Resolved', 'Closed', 'Rejected'].includes(f.status)).length;
        const resolved = allItems.filter(f => ['Resolved', 'Closed'].includes(f.status)).length;

        setStats({ total, pending, resolved, avgTime: null });
        setRecentFeedbacks(recentRes.data || []);
      } 
      else if (isCustomer) {
        // Customer: Load own feedbacks and calculate stats client-side
        const [allFeedRes, recentRes] = await Promise.all([
          feedbackApi.getMyFeedbacks({ pageSize: 100 }).catch(() => ({ data: [] })),
          feedbackApi.getMyFeedbacks({ pageSize: 5 }).catch(() => ({ data: [] }))
        ]);

        const allItems = allFeedRes.data || [];
        const total = allItems.length;
        const pending = allItems.filter(f => !['Resolved', 'Closed', 'Rejected'].includes(f.status)).length;
        const resolved = allItems.filter(f => ['Resolved', 'Closed'].includes(f.status)).length;

        setStats({ total, pending, resolved, avgTime: null });
        setRecentFeedbacks(recentRes.data || []);
      }
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
      setError('Đã xảy ra lỗi khi tải dữ liệu dashboard.');
    } finally {
      setLoading(false);
    }
  }, [isAdminOrManager, isStaffOnly, isCustomer]);

  useEffect(() => {
    fetchDashboardData();
  }, [fetchDashboardData]);

  if (loading) {
    return (
      <div className="loading-screen">
        <div className="spinner" />
        <p>Đang tải dữ liệu dashboard...</p>
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      <div className="page-header flex-between" style={{ flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1>Dashboard</h1>
          <p>Xin chào, <span className="font-semibold text-primary">{user?.fullName || user?.email}</span>! Chào mừng bạn quay trở lại.</p>
        </div>
        <button className="btn btn-secondary btn-sm" onClick={fetchDashboardData}>
          🔄 Làm mới
        </button>
      </div>

      {error && (
        <div className="alert alert-error mb-4">
          {error}
        </div>
      )}

      {/* Grid statistics cards */}
      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--primary-glow)', color: 'var(--primary-light)' }}>
            📊
          </div>
          <div className="stat-info">
            <div className="stat-value">{stats.total}</div>
            <div className="stat-label">
              {isCustomer ? 'Phản hồi đã gửi' : (isStaffOnly ? 'Phản hồi được giao' : 'Tổng số phản hồi')}
            </div>
          </div>
        </div>
        
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--warning-light)', color: 'var(--warning)' }}>
            ⏳
          </div>
          <div className="stat-info">
            <div className="stat-value">{stats.pending}</div>
            <div className="stat-label">Đang chờ xử lý</div>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'var(--success-light)', color: 'var(--success)' }}>
            ✅
          </div>
          <div className="stat-info">
            <div className="stat-value">{stats.resolved}</div>
            <div className="stat-label">Đã giải quyết</div>
          </div>
        </div>

        {stats.avgTime !== null && (
          <div className="stat-card">
            <div className="stat-icon" style={{ background: 'var(--info-light)', color: 'var(--info)' }}>
              ⏱️
            </div>
            <div className="stat-info">
              <div className="stat-value">{Number(stats.avgTime).toFixed(1)}h</div>
              <div className="stat-label">Thời gian giải quyết TB</div>
            </div>
          </div>
        )}
      </div>

      {/* Recent Activity Table / List */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Hoạt động gần đây</h2>
          <span className="text-muted" style={{ fontSize: '0.8rem' }}>5 phản hồi mới nhất</span>
        </div>
        
        {recentFeedbacks.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📭</div>
            <h3>Chưa có dữ liệu</h3>
            <p>Không tìm thấy phản hồi gần đây nào của bạn.</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Mã phiếu</th>
                  <th>Tiêu đề</th>
                  <th>Danh mục</th>
                  <th>Ưu tiên</th>
                  <th>Trạng thái</th>
                  <th>Ngày gửi</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {recentFeedbacks.map(item => (
                  <tr key={item.id}>
                    <td className="font-semibold text-muted" style={{ fontSize: '0.8rem' }}>
                      #{item.id?.substring?.(0, 8) || item.id}
                    </td>
                    <td className="truncate font-semibold" style={{ maxWidth: 200 }}>
                      {item.title}
                    </td>
                    <td>{item.category || item.category?.name || '---'}</td>
                    <td>
                      <span className={`badge badge-${priorityBadge(item.priority)}`}>
                        {item.priority || 'Medium'}
                      </span>
                    </td>
                    <td>
                      <span className={`badge badge-${statusBadge(item.status)}`}>
                        {item.status}
                      </span>
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {item.createdAtUtc 
                        ? new Date(item.createdAtUtc).toLocaleDateString('vi-VN') 
                        : item.createdAt 
                          ? new Date(item.createdAt).toLocaleDateString('vi-VN') 
                          : '---'}
                    </td>
                    <td>
                      <Link to={`/feedbacks/${item.id}`} className="btn btn-sm btn-secondary" style={{ padding: '4px 10px', fontSize: '0.75rem' }}>
                        Xem chi tiết
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default DashboardPage;
