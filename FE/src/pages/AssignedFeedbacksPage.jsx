import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';
import { getAssignedListActionLabel, getFeedbackActionPolicy } from '../utils/feedbackActions';

const priorityBadge = (priority) => {
  if (priority === 'Urgent' || priority === 'High') return 'danger';
  if (priority === 'Medium') return 'warning';
  return 'info';
};

const statusBadge = (status) => {
  switch (status) {
    case 'Submitted': return 'info';
    case 'Assigned': return 'primary';
    case 'InProgress': return 'warning';
    case 'Resolved': return 'success';
    case 'Closed': return 'gray';
    case 'Cancelled': return 'danger';
    default: return 'primary';
  }
};

const AssignedFeedbacksPage = () => {
  const { user, hasRole } = useAuth();
  const isManager = hasRole('DepartmentManager');

  const [feedbacks, setFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [staffList, setStaffList] = useState([]);
  const [message, setMessage] = useState({ text: '', type: '' });

  // Filters (Manager/Admin only)
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [categories, setCategories] = useState([]);
  const [page, setPage] = useState(1);
  const [pagination, setPagination] = useState({ totalPages: 1, totalCount: 0 });

  // Assignment state
  const [assigningId, setAssigningId] = useState(null);
  const [selectedStaff, setSelectedStaff] = useState('');
  const [assignLoading, setAssignLoading] = useState(false);

  const fetchFeedbacks = useCallback(async () => {
    setLoading(true);
    try {
      const params = { page, pageSize: 20 };
      if (statusFilter) params.status = statusFilter;
      if (priorityFilter) params.priority = priorityFilter;
      if (categoryFilter) params.categoryId = categoryFilter;
      if (fromDate) params.fromDate = fromDate;
      if (toDate) params.toDate = toDate;
      if (searchTerm) params.searchTerm = searchTerm;
      const res = await feedbackApi.getAssignedFeedbacks(params);
      setFeedbacks(res.data || []);
      setPagination(res.pagination || { totalPages: 1, totalCount: res.data?.length || 0 });
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [categoryFilter, fromDate, page, priorityFilter, searchTerm, statusFilter, toDate]);

  useEffect(() => {
    fetchFeedbacks();
  }, [fetchFeedbacks]);

  useEffect(() => {
    feedbackApi.getCategories()
      .then(res => setCategories(res.data || []))
      .catch(err => console.error('Failed to load categories:', err));
  }, []);

  useEffect(() => {
    if (isManager) {
      userApi.getSupportStaff()
        .then(res => setStaffList(res.data || []))
        .catch(err => console.error('Failed to load support staff:', err));
    }
  }, [isManager]);

  const showMessage = (text, type = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage({ text: '', type: '' }), 4000);
  };

  const handleAssign = async (feedback) => {
    if (!selectedStaff) {
      showMessage('Vui lòng chọn nhân viên hỗ trợ.', 'error');
      return;
    }
    setAssignLoading(true);
    try {
      const assignmentRequest = { assignToUserId: selectedStaff };
      if (feedback.status === 'Submitted') {
        await feedbackApi.assignFeedback(feedback.id, assignmentRequest);
      } else {
        await feedbackApi.reassignFeedback(feedback.id, assignmentRequest);
      }
      showMessage('Giao phản hồi thành công!');
      setAssigningId(null);
      setSelectedStaff('');
      fetchFeedbacks();
    } catch (err) {
      showMessage('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setAssignLoading(false);
    }
  };

  const canAssign = (item) => {
    const policy = getFeedbackActionPolicy(user, item);
    return policy.canAssign || policy.canReassign;
  };

  return (
    <div className="assigned-feedbacks-page">
      <div className="page-header">
        <h1>{isManager ? 'Quản Lý Phản Hồi' : 'Phản Hồi Được Giao'}</h1>
        <p>{isManager ? 'Xem và giao phản hồi cho nhân viên hỗ trợ.' : 'Danh sách các phản hồi bạn cần xử lý.'}</p>
      </div>

      {message.text && (
        <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>
          {message.text}
        </div>
      )}

      {/* Feedback filters */}
      {(
        <div className="card mb-4">
          <div className="card-header">
            <h3 className="card-title">Bộ lọc</h3>
          </div>
          <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', padding: '0 1.5rem 1.5rem' }}>
            <div className="form-group" style={{ flex: 1, minWidth: 180, marginBottom: 0 }}>
              <label className="form-label">Trạng thái</label>
              <select
                id="filter-status"
                className="form-control"
                value={statusFilter}
                onChange={e => { setStatusFilter(e.target.value); setPage(1); }}
              >
                <option value="">Tất cả</option>
                <option value="Submitted">Submitted</option>
                <option value="Assigned">Assigned</option>
                <option value="InProgress">InProgress</option>
                <option value="Resolved">Resolved</option>
                <option value="Cancelled">Cancelled</option>
                <option value="Closed">Closed</option>
              </select>
            </div>
            <div className="form-group" style={{ flex: 1, minWidth: 180, marginBottom: 0 }}>
              <label className="form-label">Mức độ ưu tiên</label>
              <select
                id="filter-priority"
                className="form-control"
                value={priorityFilter}
                onChange={e => { setPriorityFilter(e.target.value); setPage(1); }}
              >
                <option value="">Tất cả</option>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>
            <div className="form-group" style={{ flex: 2, minWidth: 200, marginBottom: 0 }}>
              <label className="form-label">Tìm kiếm</label>
              <input
                id="filter-search"
                type="text"
                className="form-control"
                placeholder="Nhập từ khóa..."
                value={searchTerm}
                onChange={e => { setSearchTerm(e.target.value); setPage(1); }}
              />
            </div>
            <div className="form-group" style={{ flex: 1, minWidth: 180, marginBottom: 0 }}>
              <label className="form-label">Danh mục</label>
              <select
                className="form-control"
                value={categoryFilter}
                onChange={e => { setCategoryFilter(e.target.value); setPage(1); }}
              >
                <option value="">Tất cả</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </div>
            <div className="form-group" style={{ flex: 1, minWidth: 160, marginBottom: 0 }}>
              <label className="form-label">Từ ngày</label>
              <input className="form-control" type="date" value={fromDate} onChange={e => { setFromDate(e.target.value); setPage(1); }} />
            </div>
            <div className="form-group" style={{ flex: 1, minWidth: 160, marginBottom: 0 }}>
              <label className="form-label">Đến ngày</label>
              <input className="form-control" type="date" value={toDate} onChange={e => { setToDate(e.target.value); setPage(1); }} />
            </div>
          </div>
        </div>
      )}

      <div className="card">
        {loading ? (
           <div className="loading-screen" style={{ height: 200 }}>
             <div className="spinner" />
           </div>
        ) : feedbacks.length === 0 ? (
          <div className="empty-state">
            <div className="empty-icon">☕</div>
            <h3>Không có phản hồi nào</h3>
            <p>{isManager ? 'Không tìm thấy phản hồi phù hợp.' : 'Tuyệt vời! Bạn đã xử lý xong mọi thứ.'}</p>
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
                  {isManager && <th>Người Xử Lý</th>}
                  <th>Hành Động</th>
                </tr>
              </thead>
              <tbody>
                {feedbacks.map(item => (
                  <tr key={item.id}>
                    <td className="font-semibold">#{item.id?.substring?.(0, 8) || item.id}</td>
                    <td className="truncate" style={{ maxWidth: 200 }}>{item.title}</td>
                    <td>
                      <span className={`badge badge-${priorityBadge(item.priority)}`}>
                         {item.priority || 'Medium'}
                      </span>
                    </td>
                    <td>
                      <span className={`badge badge-${statusBadge(item.status)}`}>
                        {item.status}
                      </span>
                    </td>
                    <td>{item.submittedByUserName || item.customer?.fullName || item.customer?.email || '---'}</td>
                    {isManager && (
                      <td>{item.assignedToUserName || <span className="text-muted">Chưa giao</span>}</td>
                    )}
                    <td style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
                      <Link to={`/feedbacks/${item.id}`} className="btn btn-sm btn-primary">
                        {getAssignedListActionLabel(user?.role, item.status)}
                      </Link>
                      {canAssign(item) && assigningId !== item.id && (
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => { setAssigningId(item.id); setSelectedStaff(''); }}
                        >
                          Giao việc
                        </button>
                      )}
                    </td>
                  </tr>
                ))}

                {/* Inline assignment row */}
                {feedbacks.map(item => (
                  assigningId === item.id && (
                    <tr key={`assign-${item.id}`} style={{ background: 'var(--bg-input)' }}>
                      <td colSpan={isManager ? 7 : 6}>
                        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', padding: '0.5rem 0' }}>
                          <span className="font-semibold">Giao cho:</span>
                          <select
                            id={`assign-staff-${item.id}`}
                            className="form-control"
                            style={{ flex: 1, maxWidth: 300 }}
                            value={selectedStaff}
                            onChange={e => setSelectedStaff(e.target.value)}
                          >
                            <option value="">-- Chọn nhân viên --</option>
                            {staffList.map(s => (
                              <option key={s.id} value={s.id}>
                                {s.fullName || `${s.firstName} ${s.lastName}`} ({s.email})
                              </option>
                            ))}
                          </select>
                          <button
                            className="btn btn-sm btn-primary"
                            disabled={assignLoading || !selectedStaff}
                            onClick={() => handleAssign(item)}
                          >
                            {assignLoading ? 'Đang giao...' : 'Xác nhận'}
                          </button>
                          <button
                            className="btn btn-sm btn-secondary"
                            onClick={() => setAssigningId(null)}
                          >
                            Hủy
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                ))}
              </tbody>
            </table>
          </div>
        )}
        {!loading && (
          <div className="flex-between" style={{ marginTop: '1rem' }}>
            <button className="btn btn-secondary" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Trang trước</button>
            <span>{pagination.totalCount} phản hồi · Trang {page}/{pagination.totalPages || 1}</span>
            <button className="btn btn-secondary" disabled={page >= (pagination.totalPages || 1)} onClick={() => setPage(value => value + 1)}>Trang sau</button>
          </div>
        )}
      </div>
    </div>
  );
};

export default AssignedFeedbacksPage;
