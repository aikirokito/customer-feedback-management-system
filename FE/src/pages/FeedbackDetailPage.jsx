import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';

const isClosed = (status) => status === 'Closed';
const isConversationLocked = (status) => status === 'Closed' || status === 'Cancelled';

// Status lifecycle map — backend enforced, FE mirrors for UX
const STATUS_TRANSITIONS = {
  Submitted: ['Assigned', 'Cancelled'],
  Assigned: ['InProgress'],
  InProgress: ['Resolved'],
  Resolved: ['Closed'],
  Closed: [],
  Cancelled: [],
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
  const [isInternalReply, setIsInternalReply] = useState(false);
  const [replyParentId, setReplyParentId] = useState(null);
  const [rating, setRating] = useState('');
  const [ratingLoading, setRatingLoading] = useState(false);
  const [attachmentFiles, setAttachmentFiles] = useState([]);
  const [attachmentLoading, setAttachmentLoading] = useState(false);
  const [assignmentHistory, setAssignmentHistory] = useState([]);
  const [categories, setCategories] = useState([]);
  const [editingFeedback, setEditingFeedback] = useState(false);
  const [editForm, setEditForm] = useState({ title: '', description: '', categoryId: '', priority: 'Medium' });
  const [editLoading, setEditLoading] = useState(false);

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

  const fetchDetail = useCallback(async () => {
    try {
      const res = await feedbackApi.getFeedbackById(id);
      setFeedback(res.data);
      setNewPriority(res.data?.priority || 'Medium');
      setRating(res.data?.rating ? String(res.data.rating) : '');
      setEditForm({
        title: res.data?.title || '',
        description: res.data?.description || '',
        categoryId: res.data?.categoryId || '',
        priority: res.data?.priority || 'Medium',
      });
      setError('');
    } catch (err) {
      setError(err.normalizedMessage || 'Không thể tải chi tiết phản hồi.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void fetchDetail();
  }, [fetchDetail]);

  useEffect(() => {
    if (isManager) {
      userApi.getSupportStaff()
        .then(res => setStaffList(res.data || []))
        .catch(err => console.error('Failed to load staff:', err));
    }
  }, [isManager]);

  useEffect(() => {
    if (isStaff) {
      feedbackApi.getCategories().then((response) => setCategories(response.data || [])).catch(() => setCategories([]));
      feedbackApi.getAssignmentHistory(id)
        .then((response) => setAssignmentHistory(response.data || []))
        .catch(() => setAssignmentHistory([]));
    }
  }, [id, isStaff, feedback?.assignedToUserId]);

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
        await feedbackApi.respondToFeedback(id, { content: replyContent, isInternal: isInternalReply });
      } else {
        await feedbackApi.addComment(id, { content: replyContent, parentCommentId: replyParentId });
      }
      setReplyContent('');
      setReplyParentId(null);
      setIsInternalReply(false);
      fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi khi gửi phản hồi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setSubmittingReply(false);
    }
  };

  const handleStatusChange = async () => {
    if (!nextStatus) return;
    if (nextStatus === 'Closed' && !statusReason.trim()) {
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

  const handleUnassign = async () => {
    if (!window.confirm('Gỡ phân công và đưa phiếu về hàng đợi mới?')) return;
    setAssignLoading(true);
    try {
      await feedbackApi.unassignFeedback(id);
      showWfMessage('Đã gỡ phân công và đưa phiếu về trạng thái Submitted.');
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setAssignLoading(false);
    }
  };

  const handleRate = async () => {
    if (!rating) return;
    setRatingLoading(true);
    try {
      await feedbackApi.rateFeedback(id, rating);
      showWfMessage('Cảm ơn bạn đã đánh giá chất lượng hỗ trợ.');
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setRatingLoading(false);
    }
  };

  const handleUploadAttachments = async () => {
    if (attachmentFiles.length === 0) return;
    setAttachmentLoading(true);
    try {
      await Promise.all(attachmentFiles.map((file) => feedbackApi.uploadAttachment(id, file)));
      setAttachmentFiles([]);
      showWfMessage('Đã tải tệp đính kèm.');
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setAttachmentLoading(false);
    }
  };

  const handleDeleteAttachment = async (attachment) => {
    if (!window.confirm(`Xóa tệp "${attachment.fileName}"?`)) return;
    try {
      await feedbackApi.deleteAttachment(id, attachment.id);
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    }
  };

  const editConversationItem = async (kind, item) => {
    const content = window.prompt('Cập nhật nội dung:', item.content);
    if (!content?.trim() || content.trim() === item.content) return;
    try {
      if (kind === 'response') await feedbackApi.updateResponse(id, item.id, content.trim());
      else await feedbackApi.updateComment(id, item.id, content.trim());
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    }
  };

  const deleteConversationItem = async (kind, item) => {
    if (!window.confirm('Xóa nội dung này?')) return;
    try {
      if (kind === 'response') await feedbackApi.deleteResponse(id, item.id);
      else await feedbackApi.deleteComment(id, item.id);
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    }
  };

  const handleDeleteFeedback = async () => {
    if (!window.confirm(`Xóa phản hồi "${feedback.title}"?`)) return;
    try {
      await feedbackApi.cancelFeedback(id);
      navigate(isAdmin ? '/assigned-feedbacks' : '/my-feedbacks', { replace: true });
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    }
  };

  const handleEditFeedback = async (event) => {
    event.preventDefault();
    setEditLoading(true);
    try {
      await feedbackApi.updateFeedback(id, {
        title: editForm.title.trim(),
        description: editForm.description.trim(),
        categoryId: editForm.categoryId,
        priority: editForm.priority,
      });
      setEditingFeedback(false);
      showWfMessage('Đã cập nhật nội dung phản hồi.');
      await fetchDetail();
    } catch (err) {
      showWfMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setEditLoading(false);
    }
  };

  if (loading) return <div className="loading-screen"><div className="spinner" /></div>;
  if (error) return <div className="alert alert-error m-4">{error}</div>;
  if (!feedback) return <div className="empty-state">Phản hồi không tồn tại</div>;

  const availableStatuses = (STATUS_TRANSITIONS[feedback.status] || []).filter((status) => status !== 'Assigned');
  const needsReason = nextStatus === 'Closed';
  const canRate = !isStaff && ['Resolved', 'Closed'].includes(feedback.status);
  const isAdmin = hasRole('SystemAdmin');
  const canDeleteFeedback = !isStaff && feedback.status === 'Submitted';
  const renderComment = (comment, depth = 0) => {
    const canModify = isAdmin || (!isConversationLocked(feedback.status) && comment.authorUserId === user?.id);
    return (
      <div key={comment.id} style={{ marginLeft: Math.min(depth, 3) * 24 }}>
        <div className="mb-4 p-3" style={{ borderLeft: '3px solid var(--success)', background: 'var(--bg-input)', borderRadius: '0 var(--radius-sm) var(--radius-sm) 0' }}>
          <div className="flex-between mb-2" style={{ gap: '1rem' }}>
            <span className="font-semibold">{comment.authorName || 'Người dùng'}</span>
            <span className="text-muted" style={{ fontSize: '0.8rem' }}>{comment.createdAtUtc ? new Date(comment.createdAtUtc).toLocaleString('vi-VN') : '---'}</span>
          </div>
          <p style={{ whiteSpace: 'pre-wrap' }}>{comment.content}</p>
          <div className="flex gap-2" style={{ marginTop: '0.75rem' }}>
            {!isStaff && !isConversationLocked(feedback.status) && <button className="btn btn-sm btn-secondary" type="button" onClick={() => setReplyParentId(comment.id)}>Trả lời</button>}
            {canModify && <button className="btn btn-sm btn-secondary" type="button" onClick={() => editConversationItem('comment', comment)}>Sửa</button>}
            {canModify && <button className="btn btn-sm btn-danger" type="button" onClick={() => deleteConversationItem('comment', comment)}>Xóa</button>}
          </div>
        </div>
        {comment.replies?.map((reply) => renderComment(reply, depth + 1))}
      </div>
    );
  };

  return (
    <div className="feedback-detail-page">
      <div className="page-header flex-between">
        <div>
          <h1>Chi Tiết Phản Hồi #{feedback.id?.substring?.(0, 8) || feedback.id}</h1>
          <p>Gửi lúc {feedback.createdAtUtc ? new Date(feedback.createdAtUtc).toLocaleString('vi-VN') : feedback.createdAt ? new Date(feedback.createdAt).toLocaleString('vi-VN') : '---'}</p>
        </div>
        <div className="flex gap-2">
          {canDeleteFeedback && <button className="btn btn-danger" onClick={handleDeleteFeedback}>Xóa phản hồi</button>}
          <button className="btn btn-secondary" onClick={() => navigate(-1)}>⬅ Quay lại</button>
        </div>
      </div>

      {wfMessage.text && (
        <div className={`alert alert-${wfMessage.type === 'error' ? 'error' : 'success'} mb-4`}>{wfMessage.text}</div>
      )}

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
        {feedback.attachments?.length > 0 && (
          <div style={{ marginTop: '1rem' }}>
            <h3 style={{ fontSize: '1rem', marginBottom: '0.5rem' }}>Tệp đính kèm</h3>
            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
              {feedback.attachments.map((attachment) => <div key={attachment.id} className="flex gap-2">
                <a className="btn btn-sm btn-secondary" href={attachment.publicUrl} target="_blank" rel="noreferrer">{attachment.fileName}</a>
                {(isAdmin || (!isConversationLocked(feedback.status) && attachment.uploadedByUserId === user?.id)) && (
                  <button className="btn btn-sm btn-danger" type="button" onClick={() => handleDeleteAttachment(attachment)}>Xóa</button>
                )}
              </div>)}
            </div>
          </div>
        )}
        {!isConversationLocked(feedback.status) && (feedback.attachments?.length || 0) < 3 && (
          <div style={{ marginTop: '1rem', display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
            <input className="form-control" style={{ maxWidth: 420 }} type="file" multiple accept=".jpg,.jpeg,.png,.gif,.pdf,.docx,.xlsx" onChange={(event) => setAttachmentFiles(Array.from(event.target.files || []).slice(0, 3 - (feedback.attachments?.length || 0)))} />
            <button type="button" className="btn btn-secondary" disabled={attachmentLoading || attachmentFiles.length === 0} onClick={handleUploadAttachments}>{attachmentLoading ? 'Đang tải...' : 'Tải tệp'}</button>
          </div>
        )}
      </div>

      {canRate && (
        <div className="card mb-4">
          <div className="card-header"><h3 className="card-title">Đánh giá chất lượng hỗ trợ</h3></div>
          <div className="flex gap-3" style={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <select className="form-control" style={{ maxWidth: 300 }} value={rating} onChange={(event) => setRating(event.target.value)}>
              <option value="">-- Chọn mức đánh giá --</option>
              <option value="1">1 - Rất không hài lòng</option>
              <option value="2">2 - Không hài lòng</option>
              <option value="3">3 - Bình thường</option>
              <option value="4">4 - Hài lòng</option>
              <option value="5">5 - Rất hài lòng</option>
            </select>
            <button className="btn btn-primary" disabled={!rating || ratingLoading || Number(rating) === feedback.rating} onClick={handleRate}>{ratingLoading ? 'Đang lưu...' : 'Gửi đánh giá'}</button>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* Staff Workflow Panel — hidden from Customers                  */}
      {/* ============================================================ */}
      {isStaff && !isClosed(feedback.status) && (
        <div className="card mb-4">
          <div className="card-header">
            <h3 className="card-title">🔧 Quản Lý Phản Hồi</h3>
          </div>

          <div style={{ padding: '1rem 1.5rem', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <div>
              <button className="btn btn-secondary" type="button" onClick={() => setEditingFeedback((value) => !value)}>{editingFeedback ? 'Hủy chỉnh sửa' : 'Chỉnh sửa nội dung'}</button>
              {editingFeedback && (
                <form onSubmit={handleEditFeedback} style={{ marginTop: '1rem' }}>
                  <div className="form-group"><label className="form-label">Tiêu đề</label><input className="form-control" maxLength={200} required value={editForm.title} onChange={(event) => setEditForm({ ...editForm, title: event.target.value })} /></div>
                  <div className="form-group"><label className="form-label">Nội dung</label><textarea className="form-control" maxLength={5000} required value={editForm.description} onChange={(event) => setEditForm({ ...editForm, description: event.target.value })} /></div>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                    <div className="form-group"><label className="form-label">Danh mục</label><select className="form-control" required value={editForm.categoryId} onChange={(event) => setEditForm({ ...editForm, categoryId: event.target.value })}>{!categories.some((category) => category.id === feedback.categoryId) && <option value={feedback.categoryId}>{feedback.category}</option>}{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></div>
                    <div className="form-group"><label className="form-label">Ưu tiên</label><select className="form-control" value={editForm.priority} onChange={(event) => setEditForm({ ...editForm, priority: event.target.value })}>{PRIORITIES.map((priority) => <option key={priority} value={priority}>{priority}</option>)}</select></div>
                  </div>
                  <button className="btn btn-primary" disabled={editLoading}>{editLoading ? 'Đang lưu...' : 'Lưu nội dung'}</button>
                </form>
              )}
            </div>

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
                  {feedback.assignedToUserId && (
                    <button className="btn btn-danger" disabled={assignLoading} onClick={handleUnassign}>Gỡ phân công</button>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {isStaff && assignmentHistory.length > 0 && (
        <div className="card mb-4">
          <div className="card-header"><h3 className="card-title">Lịch sử phân công</h3></div>
          <div className="table-wrap"><table><thead><tr><th>Nhân viên</th><th>Người giao</th><th>Ghi chú</th><th>Thời gian</th><th>Trạng thái</th></tr></thead><tbody>
            {assignmentHistory.map((assignment) => <tr key={assignment.id}><td>{assignment.assignedToUserName}</td><td>{assignment.assignedByUserName}</td><td>{assignment.note || '---'}</td><td>{new Date(assignment.assignedAtUtc).toLocaleString('vi-VN')}</td><td><span className={`badge ${assignment.isActive ? 'badge-success' : 'badge-gray'}`}>{assignment.isActive ? 'Hiện tại' : 'Kết thúc'}</span></td></tr>)}
          </tbody></table></div>
        </div>
      )}

      {feedback.statusHistory?.length > 0 && (
        <div className="card mb-4">
          <div className="card-header"><h3 className="card-title">Lịch sử trạng thái</h3></div>
          <div className="table-wrap"><table><thead><tr><th>Thay đổi</th><th>Người thực hiện</th><th>Lý do</th><th>Thời gian</th></tr></thead><tbody>
            {feedback.statusHistory.map((entry, index) => <tr key={`${entry.changedAtUtc}-${index}`}><td>{entry.fromStatus} → {entry.toStatus}</td><td>{entry.changedByUserName}</td><td>{entry.reason || '---'}</td><td>{new Date(entry.changedAtUtc).toLocaleString('vi-VN')}</td></tr>)}
          </tbody></table></div>
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
                <span className="font-semibold text-primary">{resp.respondedByUserName || resp.responder?.fullName || 'Nhân viên hỗ trợ'} {resp.isInternal && <span className="badge badge-warning">Nội bộ</span>}</span>
                <span className="text-muted" style={{ fontSize: '0.8rem' }}>
                  {resp.createdAtUtc ? new Date(resp.createdAtUtc).toLocaleString('vi-VN') : resp.createdAt ? new Date(resp.createdAt).toLocaleString('vi-VN') : '---'}
                </span>
              </div>
              <p style={{ whiteSpace: 'pre-wrap' }}>{resp.content}</p>
              {(isAdmin || (!isConversationLocked(feedback.status) && resp.respondedByUserId === user?.id)) && <div className="flex gap-2" style={{ marginTop: '0.75rem' }}><button className="btn btn-sm btn-secondary" type="button" onClick={() => editConversationItem('response', resp)}>Sửa</button><button className="btn btn-sm btn-danger" type="button" onClick={() => deleteConversationItem('response', resp)}>Xóa</button></div>}
            </div>
          ))}

          {feedback.comments?.filter((comment) => !comment.parentCommentId).map((comment) => renderComment(comment))}

          {(!feedback.responses?.length && !feedback.comments?.length) && (
            <p className="text-muted text-center italic py-4">Chưa có trao đổi nào.</p>
          )}
        </div>

        {!isConversationLocked(feedback.status) && (
          <form onSubmit={handleReplySubmit}>
            {replyParentId && <div className="alert alert-info mb-4">Đang trả lời một bình luận. <button type="button" className="btn btn-sm btn-secondary" onClick={() => setReplyParentId(null)}>Hủy trả lời</button></div>}
            <div className="form-group">
              <textarea
                className="form-control"
                placeholder={isStaff ? 'Nhập nội dung phản hồi...' : 'Nhập bình luận của bạn...'}
                value={replyContent}
                onChange={e => setReplyContent(e.target.value)}
                rows={4}
              />
            </div>
            {isStaff && <label className="flex gap-2 mb-4" style={{ alignItems: 'center' }}><input type="checkbox" checked={isInternalReply} onChange={(event) => setIsInternalReply(event.target.checked)} /> Ghi chú nội bộ (khách hàng không nhìn thấy)</label>}
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
