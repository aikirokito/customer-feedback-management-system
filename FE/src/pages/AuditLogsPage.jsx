import { useState, useEffect, useCallback } from 'react';
import userApi from '../api/userApi';

const AUDIT_ACTIONS = [
  { value: 'Create', label: 'Tạo mới (Create)' },
  { value: 'Update', label: 'Cập nhật (Update)' },
  { value: 'Delete', label: 'Xóa (Delete)' },
  { value: 'Login', label: 'Đăng nhập (Login)' },
  { value: 'Logout', label: 'Đăng xuất (Logout)' },
  { value: 'StatusChange', label: 'Thay đổi trạng thái (StatusChange)' },
  { value: 'Assignment', label: 'Phân công (Assignment)' },
  { value: 'Export', label: 'Xuất dữ liệu (Export)' },
];

const AuditLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 15,
    totalCount: 0,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filters
  const [actionFilter, setActionFilter] = useState('');
  const [entityTypeFilter, setEntityTypeFilter] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedLog, setSelectedLog] = useState(null); // for showing details modal

  const fetchLogs = useCallback(async (page = 1) => {
    setLoading(true);
    setError('');
    try {
      const params = {
        page,
        pageSize: 15,
      };
      if (actionFilter) params.action = actionFilter;
      if (entityTypeFilter.trim()) params.entityType = entityTypeFilter.trim();
      if (fromDate) params.fromDate = new Date(fromDate).toISOString();
      if (toDate) params.toDate = new Date(toDate).toISOString();

      const res = await userApi.getAuditLogs(params);
      setLogs(res.data || []);
      if (res.pagination) {
        setPagination(res.pagination);
      }
    } catch (err) {
      console.error(err);
      setError('Không thể tải nhật ký hệ thống.');
    } finally {
      setLoading(false);
    }
  }, [actionFilter, entityTypeFilter, fromDate, toDate]);

  useEffect(() => {
    fetchLogs(currentPage);
  }, [currentPage, fetchLogs]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setCurrentPage(1);
    fetchLogs(1);
  };

  const handleResetFilters = () => {
    setActionFilter('');
    setEntityTypeFilter('');
    setFromDate('');
    setToDate('');
    setCurrentPage(1);
    // Fetch directly after resetting
    setTimeout(() => {
      fetchLogs(1);
    }, 0);
  };

  const formatJsonData = (jsonStr) => {
    if (!jsonStr) return '---';
    try {
      const parsed = JSON.parse(jsonStr);
      return <pre style={{ margin: 0, fontSize: '0.8rem', whiteSpace: 'pre-wrap' }}>{JSON.stringify(parsed, null, 2)}</pre>;
    } catch {
      return jsonStr;
    }
  };

  return (
    <div className="audit-logs-page">
      <div className="page-header flex-between" style={{ flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1>Nhật Ký Hệ Thống</h1>
          <p>Theo dõi và giám sát toàn bộ hoạt động của hệ thống.</p>
        </div>
        <button className="btn btn-secondary btn-sm" onClick={() => fetchLogs(currentPage)}>
          🔄 Làm mới
        </button>
      </div>

      {error && <div className="alert alert-error mb-4">{error}</div>}

      {/* Advanced Filters */}
      <form onSubmit={handleSearchSubmit} className="card mb-4">
        <div className="card-header">
          <h3 className="card-title">Bộ lọc tìm kiếm nhật ký</h3>
        </div>
        <div style={{ padding: '0 1.5rem 1.5rem' }}>
          <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
            <div className="form-group" style={{ flex: 1, minWidth: 150, marginBottom: 0 }}>
              <label className="form-label">Loại hành động</label>
              <select
                id="audit-action-select"
                className="form-control"
                value={actionFilter}
                onChange={e => setActionFilter(e.target.value)}
              >
                <option value="">Tất cả</option>
                {AUDIT_ACTIONS.map(action => (
                  <option key={action.value} value={action.value}>
                    {action.label}
                  </option>
                ))}
              </select>
            </div>
            
            <div className="form-group" style={{ flex: 1, minWidth: 150, marginBottom: 0 }}>
              <label className="form-label">Đối tượng (Entity Type)</label>
              <input
                id="audit-entity-input"
                type="text"
                className="form-control"
                placeholder="Ví dụ: User, Feedback..."
                value={entityTypeFilter}
                onChange={e => setEntityTypeFilter(e.target.value)}
              />
            </div>

            <div className="form-group" style={{ flex: 1, minWidth: 150, marginBottom: 0 }}>
              <label className="form-label">Từ ngày</label>
              <input
                id="audit-from-date"
                type="date"
                className="form-control"
                value={fromDate}
                onChange={e => setFromDate(e.target.value)}
              />
            </div>

            <div className="form-group" style={{ flex: 1, minWidth: 150, marginBottom: 0 }}>
              <label className="form-label">Đến ngày</label>
              <input
                id="audit-to-date"
                type="date"
                className="form-control"
                value={toDate}
                onChange={e => setToDate(e.target.value)}
              />
            </div>

            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-end', minWidth: 200, flex: 1 }}>
              <button
                id="btn-apply-audit-filters"
                type="submit"
                className="btn btn-primary"
                style={{ flex: 1, height: '45px' }}
                disabled={loading}
              >
                Lọc
              </button>
              <button
                id="btn-reset-audit-filters"
                type="button"
                className="btn btn-secondary"
                style={{ flex: 1, height: '45px' }}
                onClick={handleResetFilters}
              >
                Đặt lại
              </button>
            </div>
          </div>
        </div>
      </form>

      {/* Audit Logs Table */}
      <div className="card">
        {loading ? (
          <div className="loading-screen" style={{ height: 300 }}><div className="spinner" /></div>
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Mã Log</th>
                    <th>Hành Động</th>
                    <th>Người Thực Hiện</th>
                    <th>Đối Tượng</th>
                    <th>Chi Tiết</th>
                    <th>Ngày Tạo</th>
                    <th>Chi Tiết</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="text-center py-4 text-muted">Không tìm thấy bản ghi nhật ký nào.</td>
                    </tr>
                  ) : (
                    logs.map(log => (
                      <tr key={log.id}>
                        <td className="font-semibold text-muted" style={{ fontSize: '0.8rem' }}>
                          {log.id?.substring?.(0, 8) || log.id}
                        </td>
                        <td>
                          <span className={`badge badge-${
                            log.action === 'Create' ? 'success' : 
                            log.action === 'Delete' ? 'danger' : 
                            log.action === 'Update' ? 'warning' : 'info'
                          }`}>
                            {log.action}
                          </span>
                        </td>
                        <td>
                          <span className="font-semibold">{log.userFullName || 'Hệ thống'}</span>
                          {log.ipAddress && <div className="text-muted" style={{ fontSize: '0.75rem' }}>IP: {log.ipAddress}</div>}
                        </td>
                        <td>
                          <span className="font-semibold">{log.entityType}</span>
                          {log.entityId && <div className="text-muted" style={{ fontSize: '0.75rem' }}>ID: #{log.entityId?.substring?.(0, 8)}</div>}
                        </td>
                        <td className="truncate" style={{ maxWidth: 220 }}>
                          {log.newValues || log.oldValues || '---'}
                        </td>
                        <td style={{ fontSize: '0.85rem' }}>
                          {log.createdAtUtc ? new Date(log.createdAtUtc).toLocaleString('vi-VN') : '---'}
                        </td>
                        <td>
                          <button
                            className="btn btn-sm btn-secondary"
                            onClick={() => setSelectedLog(log)}
                            style={{ padding: '4px 10px', fontSize: '0.75rem' }}
                          >
                            Xem
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            {pagination.totalPages > 1 && (
              <div className="flex-between mt-4" style={{ borderTop: '1px solid var(--border)', paddingTop: '1rem', flexWrap: 'wrap', gap: '1rem' }}>
                <span className="text-muted" style={{ fontSize: '0.85rem' }}>
                  Hiển thị trang <strong>{pagination.page}</strong> / <strong>{pagination.totalPages}</strong> (Tổng số <strong>{pagination.totalCount}</strong> bản ghi)
                </span>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button
                    id="btn-prev-audit-page"
                    className="btn btn-sm btn-secondary"
                    disabled={!pagination.hasPreviousPage}
                    onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                  >
                    ⬅ Trang trước
                  </button>
                  <button
                    id="btn-next-audit-page"
                    className="btn btn-sm btn-secondary"
                    disabled={!pagination.hasNextPage}
                    onClick={() => setCurrentPage(prev => Math.min(prev + 1, pagination.totalPages))}
                  >
                    Trang sau ➡
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Details Modal */}
      {selectedLog && (
        <div className="modal-overlay" onClick={() => setSelectedLog(null)}>
          <div className="modal" onClick={e => e.stopPropagation()} style={{ maxWidth: '650px' }}>
            <div className="modal-header">
              <h3 className="modal-title">Chi Tiết Nhật Ký Hệ Thống</h3>
              <button className="modal-close" onClick={() => setSelectedLog(null)}>✕</button>
            </div>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div>
                <strong>Hành động:</strong> <span className="badge badge-info" style={{ marginLeft: '0.5rem' }}>{selectedLog.action}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
                <div><strong>Người thực hiện:</strong> {selectedLog.userFullName || 'Hệ thống'}</div>
                <div><strong>Địa chỉ IP:</strong> {selectedLog.ipAddress || '---'}</div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
                <div><strong>Đối tượng (Entity):</strong> {selectedLog.entityType}</div>
                <div><strong>Mã đối tượng (Entity ID):</strong> {selectedLog.entityId || '---'}</div>
              </div>
              <div>
                <strong>Thời gian:</strong> {selectedLog.createdAtUtc ? new Date(selectedLog.createdAtUtc).toLocaleString('vi-VN') : '---'}
              </div>

              {selectedLog.oldValues && (
                <div>
                  <strong className="text-danger" style={{ display: 'block', marginBottom: '0.25rem' }}>Dữ liệu cũ (Old Values):</strong>
                  <div style={{ padding: '0.75rem', background: 'rgba(239, 68, 68, 0.05)', border: '1px solid rgba(239, 68, 68, 0.1)', borderRadius: 'var(--radius-sm)' }}>
                    {formatJsonData(selectedLog.oldValues)}
                  </div>
                </div>
              )}

              {selectedLog.newValues && (
                <div>
                  <strong className="text-success" style={{ display: 'block', marginBottom: '0.25rem' }}>Dữ liệu mới (New Values):</strong>
                  <div style={{ padding: '0.75rem', background: 'rgba(16, 185, 129, 0.05)', border: '1px solid rgba(16, 185, 129, 0.1)', borderRadius: 'var(--radius-sm)' }}>
                    {formatJsonData(selectedLog.newValues)}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AuditLogsPage;
