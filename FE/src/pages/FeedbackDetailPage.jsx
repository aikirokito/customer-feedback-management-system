import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';

const isClosed = (status) => status === 'Closed';

// Status lifecycle map — backend enforced, FE mirrors for UX
const STATUS_TRANSITIONS = {
  New: ['Assigned', 'Rejected'],
  Assigned: ['InProgress', 'Rejected'],
  InProgress: ['WaitingForCustomer', 'Resolved', 'Rejected'],
  WaitingForCustomer: ['InProgress', 'Resolved', 'Closed'],
  Resolved: ['Closed', 'InProgress'],
  Rejected: ['Closed'],
  Closed: [],
};

const PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'];

const FeedbackDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, hasRole } = useAuth();
  
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const [replyContent, setReplyContent] = useState('');
  const [submittingReply, setSubmittingReply] = useState(false);

  // Workflow state
  const [nextStatus, setNextStatus] = useState('');
  const [statusReason, setStatusReason] = useState('');
  const [statusLoading, setStatusLoading] = useState(false);
  const [newPriority, setNewPriority] = useState('');
  const [priorityLoading, setPriorityLoading] = useState(false);
  const [staffList, setStaffList] = useState([]);
  const [assignStaff, setAssignStaff] = useState('');
  const [assignLoading, setAssignLoading] = useState(false);
  const [wfMessage, setWfMessage] = useState({ text: '', type: '' });

  const isStaff = hasRole(['SupportStaff', 'DepartmentManager', 'SystemAdmin']);
  const isManager = hasRole(['DepartmentManager', 'SystemAdmin']);

  useEffect(() => {
    fetchDetail();
  }, [id]);

  useEffect(() => {
    if (isManager) {
      userApi.getSupportStaff()
        .then(res => setStaffList(res.data || []))
        .catch(err => console.error('Failed to load staff:', err));
    }
  }, [isManager]);

  // Sync priority dropdown when feedback loads
  useEffect(() => {
    if (feedback) {
      setNewPriority(feedback.priority || 'Medium');
    }
  }, [feedback]);

  const fetchDetail = async () => {
    try {
      const res = await feedbackApi.getFeedbackById(id);
      setFeedback(res.data);
      setError('');
    } catch (err) {
      setError(err.normalizedMessage || 'Không thể tải chi tiết phản hồi.');
    } finally {
      setLoading(false);
    }
  };

  const showWfMessage = (text, type = 'success') => {
    setWfMessage({ text, type });
    setTimeout(() => setWfMessage({ text: '', type: '' }), 4000);
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

  const handleStatusChange = async () => {
    if (!nextStatus) return;
    if ((nextStatus === 'Rejected' || nextStatus === 'Closed') && !statusReason.trim()) {
      showWfMessage('Vui lòng nhập lý do khi từ chối hoặc đóng phản hồi.', 'error');
      return;
    }
    setStatusLoading(true);
    try {
      await feedbackApi.updateFeedbackStatus(id, {
        newStatus: nextStatus,
        reason: statusReason || null,
      });
      showWfMessage(`Trạng thái đã cập nhật thành ${nextStatus}.`);
      setNextStatus('');
      setStatusReason('');
      fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setStatusLoading(false);
    }
  };

  const handlePriorityChange = async () => {
    if (!newPriority || newPriority === feedback?.priority) return;
    setPriorityLoading(true);
    try {
      await feedbackApi.managePriority(id, { priority: newPriority });
      showWfMessage(`Mức độ ưu tiên đã cập nhật thành ${newPriority}.`);
      fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setPriorityLoading(false);
    }
  };

  const handleAssign = async () => {
    if (!assignStaff) return;
    setAssignLoading(true);
    try {
      const isReassign = feedback.assignedToUserId;
      if (isReassign) {
        await feedbackApi.reassignFeedback(id, { assignToUserId: assignStaff });
      } else {
        await feedbackApi.assignFeedback(id, { assignToUserId: assignStaff });
      }
      showWfMessage(isReassign ? 'Đã chuyển giao thành công!' : 'Đã giao thành công!');
      setAssignStaff('');
      fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setAssignLoading(false);
    }
  };

  if (loading) return <div className="loading-screen"><div className="spinner" /></div>;
  if (error) return <div className="alert alert-error m-4">{error}</div>;
  if (!feedback) return <div className="empty-state">Phản hồi không tồn tại</div>;

  const availableStatuses = STATUS_TRANSITIONS[feedback.status] || [];
  const needsReason = nextStatus === 'Rejected' || nextStatus === 'Closed';

  return (
    <div className="feedback-detail-page">
      <div className="page-header flex-between">
        <div>
          <h1>Chi Tiết Phản Hồi #{feedback.id?.substring?.(0, 8) || feedback.id}</h1>
          <p>Gửi lúc {feedback.createdAtUtc ? new Date(feedback.createdAtUtc).toLocaleString('vi-VN') : feedback.createdAt ? new Date(feedback.createdAt).toLocaleString('vi-VN') : '---'}</p>
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
          <p><strong>Người gửi:</strong> {feedback.submittedByUserName || feedback.customer?.fullName || feedback.customer?.email || '---'}</p>
          <p><strong>Danh mục:</strong> {feedback.category || feedback.category?.name || '---'}</p>
          {isStaff && <p><strong>Mức độ ưu tiên:</strong> {feedback.priority}</p>}
          <p><strong>Đánh giá:</strong> {feedback.rating ? `${feedback.rating}/5` : 'Không đánh giá'}</p>
          {feedback.assignedToUserName && <p><strong>Người xử lý:</strong> {feedback.assignedToUserName}</p>}
        </div>

        <div className="p-4" style={{ background: 'var(--bg-input)', borderRadius: 'var(--radius-sm)' }}>
          <p style={{ whiteSpace: 'pre-wrap' }}>{feedback.description}</p>
        </div>
      </div>

      {/* ============================================================ */}
      {/* Staff Workflow Panel — hidden from Customers                  */}
      {/* ============================================================ */}
      {isStaff && !isClosed(feedback.status) && (
        <div className="card mb-4">
          <div className="card-header">
            <h3 className="card-title">🔧 Quản Lý Phản Hồi</h3>
          </div>

          {wfMessage.text && (
            <div className={`alert alert-${wfMessage.type === 'error' ? 'error' : 'success'}`} style={{ margin: '0 1.5rem' }}>
              {wfMessage.text}
            </div>
          )}

          <div style={{ padding: '1rem 1.5rem', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            {/* Status Transition */}
            {availableStatuses.length > 0 && (
              <div>
                <h4 style={{ marginBottom: '0.75rem', fontSize: '0.95rem' }}>Cập nhật trạng thái</h4>
                <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'flex-start' }}>
                  <div className="form-group" style={{ flex: 1, minWidth: 180, marginBottom: 0 }}>
                    <select
                      id="next-status"
                      className="form-control"
                      value={nextStatus}
                      onChange={e => setNextStatus(e.target.value)}
                    >
                      <option value="">-- Chọn trạng thái --</option>
                      {availableStatuses.map(s => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </div>
                  <button
                    id="btn-update-status"
                    className="btn btn-primary"
                    disabled={statusLoading || !nextStatus}
                    onClick={handleStatusChange}
                  >
                    {statusLoading ? 'Đang cập nhật...' : 'Cập nhật'}
                  </button>
                </div>
                {needsReason && (
                  <div className="form-group" style={{ marginTop: '0.75rem' }}>
                    <label className="form-label">Lý do (bắt buộc)</label>
                    <textarea
                      id="status-reason"
                      className="form-control"
                      rows={3}
                      value={statusReason}
                      onChange={e => setStatusReason(e.target.value)}
                      placeholder="Nhập lý do..."
                    />
                  </div>
                )}
              </div>
            )}

            {/* Priority Management */}
            <div>
              <h4 style={{ marginBottom: '0.75rem', fontSize: '0.95rem' }}>Mức độ ưu tiên</h4>
              <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
                <select
                  id="priority-select"
                  className="form-control"
                  style={{ maxWidth: 200 }}
                  value={newPriority}
                  onChange={e => setNewPriority(e.target.value)}
                >
                  {PRIORITIES.map(p => (
                    <option key={p} value={p}>{p}</option>
                  ))}
                </select>
                <button
                  id="btn-update-priority"
                  className="btn btn-secondary"
                  disabled={priorityLoading || newPriority === feedback.priority}
                  onClick={handlePriorityChange}
                >
                  {priorityLoading ? 'Đang lưu...' : 'Lưu'}
                </button>
              </div>
            </div>

            {/* Assignment — Manager/Admin only */}
            {isManager && (
              <div>
                <h4 style={{ marginBottom: '0.75rem', fontSize: '0.95rem' }}>
                  {feedback.assignedToUserId ? 'Chuyển giao' : 'Giao việc'}
                </h4>
                <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
                  <select
                    id="assign-staff-select"
                    className="form-control"
                    style={{ flex: 1, maxWidth: 350 }}
                    value={assignStaff}
                    onChange={e => setAssignStaff(e.target.value)}
                  >
                    <option value="">-- Chọn nhân viên hỗ trợ --</option>
                    {staffList.map(s => (
                      <option key={s.id} value={s.id}>
                        {s.fullName || `${s.firstName} ${s.lastName}`} ({s.email})
                      </option>
                    ))}
                  </select>
                  <button
                    id="btn-assign"
                    className="btn btn-primary"
                    disabled={assignLoading || !assignStaff}
                    onClick={handleAssign}
                  >
                    {assignLoading ? 'Đang giao...' : (feedback.assignedToUserId ? 'Chuyển giao' : 'Giao việc')}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Conversation History */}
      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Lịch Sử Trao Đổi</h3>
        </div>
        
        <div className="responses-list mb-4">
          {feedback.responses?.length > 0 && feedback.responses.map(resp => (
            <div key={resp.id} className="mb-4 p-3" style={{ borderLeft: '3px solid var(--primary)', background: 'var(--bg-input)', borderRadius: '0 var(--radius-sm) var(--radius-sm) 0' }}>
              <div className="flex-between mb-2">
                <span className="font-semibold text-primary">{resp.respondedByUserName || resp.responder?.fullName || 'Nhân viên hỗ trợ'}</span>
                <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                  {resp.createdAtUtc ? new Date(resp.createdAtUtc).toLocaleString('vi-VN') : resp.createdAt ? new Date(resp.createdAt).toLocaleString('vi-VN') : '---'}
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
                  {comment.createdAtUtc ? new Date(comment.createdAtUtc).toLocaleString('vi-VN') : comment.createdAt ? new Date(comment.createdAt).toLocaleString('vi-VN') : '---'}
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
