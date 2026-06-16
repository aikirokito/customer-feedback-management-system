import { useState, useEffect } from 'react';
import userApi from '../api/userApi';

const AuditLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchLogs();
  }, []);

  const fetchLogs = async () => {
    try {
      const res = await userApi.getAuditLogs();
      setLogs(res.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="audit-logs-page">
      <div className="page-header">
        <h1>Nhật Ký Hệ Thống</h1>
        <p>Theo dõi các thay đổi và hoạt động trên hệ thống.</p>
      </div>

      <div className="card">
        {loading ? (
          <div className="loading-screen" style={{ height: 200 }}><div className="spinner" /></div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Hành Động</th>
                  <th>Người Thực Hiện</th>
                  <th>Đối Tượng</th>
                  <th>Chi Tiết</th>
                  <th>Ngày Tạo</th>
                </tr>
              </thead>
              <tbody>
                {logs.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-4">Không có dữ liệu log.</td></tr>
                ) : (
                  logs.map(log => (
                    <tr key={log.id}>
                      <td>{log.id}</td>
                      <td><span className="badge badge-info">{log.action}</span></td>
                      <td>{log.userFullName || log.user?.email || log.userId || 'System'}</td>
                      <td>{log.entityType}{log.entityId ? ` #${log.entityId}` : ''}</td>
                      <td className="truncate" style={{ maxWidth: 300 }}>{log.newValues || log.oldValues || JSON.stringify(log.details) || '---'}</td>
                      <td>{log.createdAtUtc ? new Date(log.createdAtUtc).toLocaleString('vi-VN') : log.createdAt ? new Date(log.createdAt).toLocaleString('vi-VN') : '---'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default AuditLogsPage;
