import { useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';

const STATUS_LABELS = {
  PENDING: { label: 'Chờ xử lý', color: 'warning' },
  IN_PROGRESS: { label: 'Đang xử lý', color: 'info' },
  RESOLVED: { label: 'Đã giải quyết', color: 'success' },
  CLOSED: { label: 'Đã đóng', color: 'gray' },
};

const MyFeedbacksPage = () => {
  const [feedbacks, setFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);
  const location = useLocation();
  const [successMsg, setSuccessMsg] = useState(location.state?.successMsg || '');

  useEffect(() => {
    fetchMyFeedbacks();
    if (successMsg) {
      setTimeout(() => setSuccessMsg(''), 5000);
    }
  }, [successMsg]);

  const fetchMyFeedbacks = async () => {
    try {
      const res = await feedbackApi.getMyFeedbacks();
      setFeedbacks(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="my-feedbacks-page">
      <div className="page-header flex-between">
        <div>
          <h1>Phản Hồi Của Tôi</h1>
          <p>Danh sách các phản hồi bạn đã gửi.</p>
        </div>
        <Link to="/submit-feedback" className="btn btn-primary">
          + Gửi phản hồi
        </Link>
      </div>

      {successMsg && <div className="alert alert-success mb-4">✅ {successMsg}</div>}

      <div className="card">
        {loading ? (
          <div className="loading-screen" style={{ height: 200 }}>
            <div className="spinner" />
          </div>
        ) : feedbacks.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">📝</div>
            <h3>Bạn chưa gửi phản hồi nào</h3>
            <p>Hãy gửi phản hồi để chúng tôi có thể hỗ trợ bạn.</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Mã Phiếu</th>
                  <th>Tiêu Đề</th>
                  <th>Danh Mục</th>
                  <th>Trạng Thái</th>
                  <th>Ngày Gửi</th>
                  <th>Hành Động</th>
                </tr>
              </thead>
              <tbody>
                {feedbacks.map(item => (
                  <tr key={item.id}>
                    <td className="font-semibold">#{item.id}</td>
                    <td className="truncate" style={{ maxWidth: 200 }}>{item.title}</td>
                    <td>{item.category?.name || '---'}</td>
                    <td>
                      <span className={`badge badge-${STATUS_LABELS[item.status]?.color || 'gray'}`}>
                        {STATUS_LABELS[item.status]?.label || item.status}
                      </span>
                    </td>
                    <td>{new Date(item.createdAt).toLocaleDateString('vi-VN')}</td>
                    <td>
                      <Link to={`/feedbacks/${item.id}`} className="btn btn-sm btn-secondary">
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

export default MyFeedbacksPage;
