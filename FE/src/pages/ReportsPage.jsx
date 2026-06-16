import { useState, useEffect } from 'react';
import userApi from '../api/userApi';

const ReportsPage = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchReports();
  }, []);

  const fetchReports = async () => {
    try {
      const res = await userApi.getReports();
      setData(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="reports-page">
      <div className="page-header">
        <h1>Báo Cáo Thống Kê</h1>
        <p>Tổng quan về hiệu suất xử lý phản hồi.</p>
      </div>

      <div className="card">
        {loading ? (
          <div className="loading-screen" style={{ height: 300 }}><div className="spinner" /></div>
        ) : !data ? (
          <div className="empty-state">
            <div className="empty-icon">📈</div>
            <h3>Chưa có dữ liệu</h3>
            <p>Chưa thể tải báo cáo hoặc chưa kết nối database.</p>
          </div>
        ) : (
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value">{data.totalFeedbacks ?? 0}</div>
                <div className="stat-label">Tổng phản hồi</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value">{data.openFeedbacks ?? 0}</div>
                <div className="stat-label">Đang mở</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value">{data.resolvedFeedbacks ?? 0}</div>
                <div className="stat-label">Đã giải quyết</div>
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-info">
                <div className="stat-value">{data.closedFeedbacks ?? 0}</div>
                <div className="stat-label">Đã đóng</div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ReportsPage;
