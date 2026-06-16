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
        ) : (
          <div className="empty-state">
            <div className="empty-icon">📈</div>
            <h3>Chức năng đang phát triển</h3>
            <p>Biểu đồ báo cáo sẽ sớm được cập nhật.</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default ReportsPage;
