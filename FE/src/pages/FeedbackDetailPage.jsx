import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import { useAuth } from '../context/AuthContext';

const FeedbackDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [replyContent, setReplyContent] = useState('');
  const [submittingReply, setSubmittingReply] = useState(false);

  const isStaff = hasRole(['SUPPORT_STAFF', 'MANAGER', 'ADMIN']);

  useEffect(() => {
    fetchDetail();
  }, [id]);

  const fetchDetail = async () => {
    try {
      const res = await feedbackApi.getFeedbackById(id);
      setFeedback(res.data);
    } catch (err) {
      setError('Không thể tải chi tiết phản hồi.');
    } finally {
      setLoading(false);
    }
  };

  const handleReplySubmit = async (e) => {
    e.preventDefault();
    if (!replyContent.trim()) return;

    setSubmittingReply(true);
    try {
      await feedbackApi.respondToFeedback(id, { content: replyContent });
      setReplyContent('');
      fetchDetail(); // reload
    } catch (err) {
      alert('Lỗi khi gửi phản hồi: ' + (err.response?.data?.message || err.message));
    } finally {
      setSubmittingReply(false);
    }
  };

  if (loading) return <div className="loading-screen"><div className="spinner" /></div>;
  if (error) return <div className="alert alert-error m-4">{error}</div>;
  if (!feedback) return <div className="empty-state">Phản hồi không tồn tại</div>;

  return (
    <div className="feedback-detail-page">
      <div className="page-header flex-between">
        <div>
          <h1>Chi Tiết Phản Hồi #{feedback.id}</h1>
          <p>Gửi lúc {new Date(feedback.createdAt).toLocaleString('vi-VN')}</p>
        </div>
        <button className="btn btn-secondary" onClick={() => navigate(-1)}>
          ⬅ Quay lại
        </button>
      </div>

      <div className="card mb-4">
        <div className="card-header">
          <h2 className="card-title">{feedback.title}</h2>
          <span className="badge badge-primary">{feedback.status}</span>
        </div>
        
        <div className="mb-4 text-muted">
          <p><strong>Người gửi:</strong> {feedback.customer?.fullName || feedback.customer?.email}</p>
          <p><strong>Danh mục:</strong> {feedback.category?.name}</p>
          {isStaff && <p><strong>Mức độ ưu tiên:</strong> {feedback.priority}</p>}
        </div>

        <div className="p-4" style={{ background: 'var(--bg-input)', borderRadius: 'var(--radius-sm)' }}>
          <p style={{ whiteSpace: 'pre-wrap' }}>{feedback.description}</p>
        </div>
      </div>

      {/* Responses */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Lịch Sử Trao Đổi</h3>
        </div>
        
        <div className="responses-list mb-4">
          {feedback.responses?.length > 0 ? (
            feedback.responses.map(resp => (
              <div key={resp.id} className="mb-4 p-3" style={{ borderLeft: '3px solid var(--primary)', background: 'var(--bg-input)', borderRadius: '0 var(--radius-sm) var(--radius-sm) 0' }}>
                <div className="flex-between mb-2">
                  <span className="font-semibold text-primary">{resp.responder?.fullName || 'Nhân viên hỗ trợ'}</span>
                  <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                    {new Date(resp.createdAt).toLocaleString('vi-VN')}
                  </span>
                </div>
                <p style={{ whiteSpace: 'pre-wrap' }}>{resp.content}</p>
              </div>
            ))
          ) : (
            <p className="text-muted text-center italic py-4">Chưa có trao đổi nào.</p>
          )}
        </div>

        {/* Reply form (for staff mostly, but customer might reply back depending on logic) */}
        {feedback.status !== 'CLOSED' && isStaff && (
          <form onSubmit={handleReplySubmit}>
            <div className="form-group">
              <textarea
                className="form-control"
                placeholder="Nhập nội dung phản hồi..."
                value={replyContent}
                onChange={e => setReplyContent(e.target.value)}
                rows={4}
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={submittingReply || !replyContent.trim()}>
              {submittingReply ? 'Đang gửi...' : 'Trả lời'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};

export default FeedbackDetailPage;
