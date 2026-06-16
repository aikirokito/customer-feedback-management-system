import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import { useAuth } from '../context/AuthContext';

const isClosed = (status) => status === 'Closed';

const FeedbackDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [replyContent, setReplyContent] = useState('');
  const [submittingReply, setSubmittingReply] = useState(false);

  const isStaff = hasRole(['SupportStaff', 'DepartmentManager', 'SystemAdmin']);

  useEffect(() => {
    fetchDetail();
  }, [id]);

  const fetchDetail = async () => {
    try {
      const res = await feedbackApi.getFeedbackById(id);
      setFeedback(res.data);
    } catch (err) {
      setError(err.normalizedMessage || 'Không thể tải chi tiết phản hồi.');
    } finally {
      setLoading(false);
    }
  };

  const handleReplySubmit = async (e) => {
    e.preventDefault();
    if (!replyContent.trim()) return;

    setSubmittingReply(true);
    try {
      if (isStaff) {
        await feedbackApi.respondToFeedback(id, { content: replyContent, isInternal: false });
      } else {
        await feedbackApi.addComment(id, { content: replyContent });
      }
      setReplyContent('');
      fetchDetail();
    } catch (err) {
      alert('Lỗi khi gửi phản hồi: ' + (err.normalizedMessage || err.message));
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
          <p>Gửi lúc {feedback.createdAtUtc ? new Date(feedback.createdAtUtc).toLocaleString('vi-VN') : '---'}</p>
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
          <p><strong>Người gửi:</strong> {feedback.submittedByUserName || '---'}</p>
          <p><strong>Danh mục:</strong> {feedback.category || '---'}</p>
          {isStaff && <p><strong>Mức độ ưu tiên:</strong> {feedback.priority}</p>}
          {feedback.assignedToUserName && <p><strong>Người xử lý:</strong> {feedback.assignedToUserName}</p>}
        </div>

        <div className="p-4" style={{ background: 'var(--bg-input)', borderRadius: 'var(--radius-sm)' }}>
          <p style={{ whiteSpace: 'pre-wrap' }}>{feedback.description}</p>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Lịch Sử Trao Đổi</h3>
        </div>
        
        <div className="responses-list mb-4">
          {feedback.responses?.length > 0 && feedback.responses.map(resp => (
            <div key={resp.id} className="mb-4 p-3" style={{ borderLeft: '3px solid var(--primary)', background: 'var(--bg-input)', borderRadius: '0 var(--radius-sm) var(--radius-sm) 0' }}>
              <div className="flex-between mb-2">
                <span className="font-semibold text-primary">{resp.respondedByUserName || 'Nhân viên hỗ trợ'}</span>
                <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                  {resp.createdAtUtc ? new Date(resp.createdAtUtc).toLocaleString('vi-VN') : '---'}
                </span>
              </div>
              <p style={{ whiteSpace: 'pre-wrap' }}>{resp.content}</p>
            </div>
          ))}

          {feedback.comments?.length > 0 && feedback.comments.map(comment => (
            <div key={comment.id} className="mb-4 p-3" style={{ borderLeft: '3px solid var(--success)', background: 'var(--bg-input)', borderRadius: '0 var(--radius-sm) var(--radius-sm) 0' }}>
              <div className="flex-between mb-2">
                <span className="font-semibold">{comment.authorName || 'Người dùng'}</span>
                <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                  {comment.createdAtUtc ? new Date(comment.createdAtUtc).toLocaleString('vi-VN') : '---'}
                </span>
              </div>
              <p style={{ whiteSpace: 'pre-wrap' }}>{comment.content}</p>
            </div>
          ))}

          {(!feedback.responses?.length && !feedback.comments?.length) && (
            <p className="text-muted text-center italic py-4">Chưa có trao đổi nào.</p>
          )}
        </div>

        {!isClosed(feedback.status) && (
          <form onSubmit={handleReplySubmit}>
            <div className="form-group">
              <textarea
                className="form-control"
                placeholder={isStaff ? 'Nhập nội dung phản hồi...' : 'Nhập bình luận của bạn...'}
                value={replyContent}
                onChange={e => setReplyContent(e.target.value)}
                rows={4}
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={submittingReply || !replyContent.trim()}>
              {submittingReply ? 'Đang gửi...' : 'Gửi'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
};

export default FeedbackDetailPage;
