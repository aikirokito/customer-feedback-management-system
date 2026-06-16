import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import { useAuth } from '../context/AuthContext';

<<<<<<< HEAD
const isClosed = (status) => status === 'Closed';

=======
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
const FeedbackDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [replyContent, setReplyContent] = useState('');
  const [submittingReply, setSubmittingReply] = useState(false);

<<<<<<< HEAD
  const isStaff = hasRole(['SupportStaff', 'DepartmentManager', 'SystemAdmin']);
=======
  const isStaff = hasRole(['SUPPORT_STAFF', 'MANAGER', 'ADMIN']);
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397

  useEffect(() => {
    fetchDetail();
  }, [id]);

  const fetchDetail = async () => {
    try {
      const res = await feedbackApi.getFeedbackById(id);
      setFeedback(res.data);
    } catch (err) {
<<<<<<< HEAD
      setError(err.normalizedMessage || 'Không thể tải chi tiết phản hồi.');
=======
      setError('Không thể tải chi tiết phản hồi.');
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    } finally {
      setLoading(false);
    }
  };

  const handleReplySubmit = async (e) => {
    e.preventDefault();
    if (!replyContent.trim()) return;

    setSubmittingReply(true);
    try {
<<<<<<< HEAD
      if (isStaff) {
        await feedbackApi.respondToFeedback(id, { content: replyContent, isInternal: false });
      } else {
        await feedbackApi.addComment(id, { content: replyContent });
      }
      setReplyContent('');
      fetchDetail();
    } catch (err) {
      alert('Lỗi khi gửi phản hồi: ' + (err.normalizedMessage || err.message));
=======
      await feedbackApi.respondToFeedback(id, { content: replyContent });
      setReplyContent('');
      fetchDetail(); // reload
    } catch (err) {
      alert('Lỗi khi gửi phản hồi: ' + (err.response?.data?.message || err.message));
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
<<<<<<< HEAD
          <p>Gửi lúc {feedback.createdAtUtc ? new Date(feedback.createdAtUtc).toLocaleString('vi-VN') : '---'}</p>
=======
          <p>Gửi lúc {new Date(feedback.createdAt).toLocaleString('vi-VN')}</p>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
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
<<<<<<< HEAD
          <p><strong>Người gửi:</strong> {feedback.submittedByUserName || '---'}</p>
          <p><strong>Danh mục:</strong> {feedback.category || '---'}</p>
          {isStaff && <p><strong>Mức độ ưu tiên:</strong> {feedback.priority}</p>}
          {feedback.assignedToUserName && <p><strong>Người xử lý:</strong> {feedback.assignedToUserName}</p>}
=======
          <p><strong>Người gửi:</strong> {feedback.customer?.fullName || feedback.customer?.email}</p>
          <p><strong>Danh mục:</strong> {feedback.category?.name}</p>
          {isStaff && <p><strong>Mức độ ưu tiên:</strong> {feedback.priority}</p>}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
        </div>

        <div className="p-4" style={{ background: 'var(--bg-input)', borderRadius: 'var(--radius-sm)' }}>
          <p style={{ whiteSpace: 'pre-wrap' }}>{feedback.description}</p>
        </div>
      </div>

<<<<<<< HEAD
=======
      {/* Responses */}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Lịch Sử Trao Đổi</h3>
        </div>
        
        <div className="responses-list mb-4">
<<<<<<< HEAD
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
=======
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
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <p className="text-muted text-center italic py-4">Chưa có trao đổi nào.</p>
          )}
        </div>

<<<<<<< HEAD
        {!isClosed(feedback.status) && (
=======
        {/* Reply form (for staff mostly, but customer might reply back depending on logic) */}
        {feedback.status !== 'CLOSED' && isStaff && (
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
          <form onSubmit={handleReplySubmit}>
            <div className="form-group">
              <textarea
                className="form-control"
<<<<<<< HEAD
                placeholder={isStaff ? 'Nhập nội dung phản hồi...' : 'Nhập bình luận của bạn...'}
=======
                placeholder="Nhập nội dung phản hồi..."
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
                value={replyContent}
                onChange={e => setReplyContent(e.target.value)}
                rows={4}
              />
            </div>
            <button type="submit" className="btn btn-primary" disabled={submittingReply || !replyContent.trim()}>
<<<<<<< HEAD
              {submittingReply ? 'Đang gửi...' : 'Gửi'}
=======
              {submittingReply ? 'Đang gửi...' : 'Trả lời'}
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            </button>
          </form>
        )}
      </div>
    </div>
  );
};

export default FeedbackDetailPage;
