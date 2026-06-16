import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';

const AssignedFeedbacksPage = () => {
  const [feedbacks, setFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchAssignedFeedbacks();
  }, []);

  const fetchAssignedFeedbacks = async () => {
    try {
      const res = await feedbackApi.getAssignedFeedbacks();
      setFeedbacks(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="assigned-feedbacks-page">
      <div className="page-header">
        <h1>Phản Hồi Được Giao</h1>
        <p>Danh sách các phản hồi bạn cần xử lý.</p>
      </div>

      <div className="card">
        {loading ? (
           <div className="loading-screen" style={{ height: 200 }}>
             <div className="spinner" />
           </div>
        ) : feedbacks.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">☕</div>
            <h3>Không có phản hồi nào</h3>
            <p>Tuyệt vời! Bạn đã xử lý xong mọi thứ.</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Mã Phiếu</th>
                  <th>Tiêu Đề</th>
                  <th>Ưu Tiên</th>
                  <th>Trạng Thái</th>
                  <th>Người Gửi</th>
                  <th>Hành Động</th>
                </tr>
              </thead>
              <tbody>
                {feedbacks.map(item => (
                  <tr key={item.id}>
                    <td className="font-semibold">#{item.id}</td>
                    <td className="truncate" style={{ maxWidth: 200 }}>{item.title}</td>
                    <td>
                      <span className={`badge badge-${item.priority === 'HIGH' ? 'danger' : item.priority === 'MEDIUM' ? 'warning' : 'info'}`}>
                         {item.priority || 'NORMAL'}
                      </span>
                    </td>
                    <td>{item.status}</td>
                    <td>{item.customer?.fullName || item.customer?.email}</td>
                    <td>
                      <Link to={`/feedbacks/${item.id}`} className="btn btn-sm btn-primary">
                        Xử lý
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

export default AssignedFeedbacksPage;
