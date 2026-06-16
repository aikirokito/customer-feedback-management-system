import { useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';

const STATUS_LABELS = {
<<<<<<< HEAD
  New: { label: 'Mới', color: 'warning' },
  Assigned: { label: 'Đã giao', color: 'info' },
  InProgress: { label: 'Đang xử lý', color: 'info' },
  WaitingForCustomer: { label: 'Chờ khách hàng', color: 'warning' },
  Resolved: { label: 'Đã giải quyết', color: 'success' },
  Rejected: { label: 'Từ chối', color: 'danger' },
  Closed: { label: 'Đã đóng', color: 'gray' },
};

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '---';

=======
  PENDING: { label: 'Chờ xử lý', color: 'warning' },
  IN_PROGRESS: { label: 'Đang xử lý', color: 'info' },
  RESOLVED: { label: 'Đã giải quyết', color: 'success' },
  CLOSED: { label: 'Đã đóng', color: 'gray' },
};

>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
const MyFeedbacksPage = () => {
  const [feedbacks, setFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);
  const location = useLocation();
  const [successMsg, setSuccessMsg] = useState(location.state?.successMsg || '');

  useEffect(() => {
    fetchMyFeedbacks();
    if (successMsg) {
<<<<<<< HEAD
      const timer = setTimeout(() => setSuccessMsg(''), 5000);
      return () => clearTimeout(timer);
=======
      setTimeout(() => setSuccessMsg(''), 5000);
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    }
  }, [successMsg]);

  const fetchMyFeedbacks = async () => {
    try {
      const res = await feedbackApi.getMyFeedbacks();
<<<<<<< HEAD
      setFeedbacks(res.data || []);
=======
      setFeedbacks(res.data);
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
<<<<<<< HEAD
                    <td>{item.category || '---'}</td>
=======
                    <td>{item.category?.name || '---'}</td>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
                    <td>
                      <span className={`badge badge-${STATUS_LABELS[item.status]?.color || 'gray'}`}>
                        {STATUS_LABELS[item.status]?.label || item.status}
                      </span>
                    </td>
<<<<<<< HEAD
                    <td>{formatDate(item.createdAtUtc)}</td>
=======
                    <td>{new Date(item.createdAt).toLocaleDateString('vi-VN')}</td>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
