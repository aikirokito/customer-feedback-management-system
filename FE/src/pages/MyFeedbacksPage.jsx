import { useCallback, useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';

const STATUS_LABELS = {
  New: 'Mới',
  Assigned: 'Đã giao',
  InProgress: 'Đang xử lý',
  WaitingForCustomer: 'Chờ khách hàng',
  Resolved: 'Đã giải quyết',
  Rejected: 'Từ chối',
  Closed: 'Đã đóng',
};

const MyFeedbacksPage = () => {
  const location = useLocation();
  const [feedbacks, setFeedbacks] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(location.state?.successMsg || '');
  const [filters, setFilters] = useState({ status: '', categoryId: '', fromDate: '', toDate: '' });
  const [page, setPage] = useState(1);
  const [pagination, setPagination] = useState({ totalPages: 1, totalCount: 0 });

  const fetchFeedbacks = useCallback(async () => {
    setLoading(true);
    try {
      const params = { page, pageSize: 20 };
      Object.entries(filters).forEach(([key, value]) => {
        if (value) params[key] = value;
      });
      const response = await feedbackApi.getMyFeedbacks(params);
      setFeedbacks(response.data || []);
      setPagination(response.pagination || { totalPages: 1, totalCount: response.data?.length || 0 });
    } finally {
      setLoading(false);
    }
  }, [filters, page]);

  useEffect(() => {
    Promise.all([feedbackApi.getCategories(), fetchFeedbacks()])
      .then(([categoryResponse]) => setCategories(categoryResponse.data || []))
      .catch((err) => setMessage(err.normalizedMessage || 'Không thể tải danh sách phản hồi.'));
  }, [fetchFeedbacks]);

  const updateFilter = (name, value) => {
    setFilters((current) => ({ ...current, [name]: value }));
    setPage(1);
  };

  const deleteFeedback = async (item) => {
    if (!window.confirm(`Xóa phản hồi "${item.title}"?`)) return;
    try {
      await feedbackApi.deleteFeedback(item.id);
      setMessage('Đã xóa phản hồi.');
      await fetchFeedbacks();
    } catch (error) {
      setMessage(error.normalizedMessage || 'Không thể xóa phản hồi.');
    }
  };

  return (
    <div className="my-feedbacks-page">
      <div className="page-header flex-between">
        <div>
          <h1>Phản hồi của tôi</h1>
          <p>Theo dõi trạng thái và phản hồi từ đội ngũ hỗ trợ.</p>
        </div>
        <Link to="/submit-feedback" className="btn btn-primary">+ Gửi phản hồi</Link>
      </div>

      {message && <div className="alert alert-success mb-4">{message}</div>}

      <div className="card mb-4">
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <select className="form-control" value={filters.status} onChange={(e) => updateFilter('status', e.target.value)}>
            <option value="">Tất cả trạng thái</option>
            {Object.entries(STATUS_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
          <select className="form-control" value={filters.categoryId} onChange={(e) => updateFilter('categoryId', e.target.value)}>
            <option value="">Tất cả danh mục</option>
            {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
          </select>
          <input type="date" className="form-control" value={filters.fromDate} onChange={(e) => updateFilter('fromDate', e.target.value)} aria-label="Từ ngày" />
          <input type="date" className="form-control" value={filters.toDate} onChange={(e) => updateFilter('toDate', e.target.value)} aria-label="Đến ngày" />
        </div>
      </div>

      <div className="card">
        {loading ? <div className="loading-screen"><div className="spinner" /></div> : feedbacks.length === 0 ? (
          <div className="empty-state"><h3>Không có phản hồi phù hợp</h3></div>
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Mã phiếu</th>
                    <th>Tiêu đề</th>
                    <th>Danh mục</th>
                    <th>Đánh giá</th>
                    <th>Trạng thái</th>
                    <th>Ngày gửi</th>
                    <th>Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  {feedbacks.map((item) => (
                    <tr key={item.id}>
                      <td>#{item.id?.slice(0, 8)}</td>
                      <td className="font-semibold">{item.title}</td>
                      <td>{item.category}</td>
                      <td>{item.rating ? `${item.rating}/5` : '---'}</td>
                      <td><span className="badge badge-info">{STATUS_LABELS[item.status] || item.status}</span></td>
                      <td>{new Date(item.createdAtUtc).toLocaleDateString('vi-VN')}</td>
                      <td><div className="flex gap-2"><Link to={`/feedbacks/${item.id}`} className="btn btn-sm btn-secondary">Chi tiết</Link>{item.status === 'New' && <button className="btn btn-sm btn-danger" type="button" onClick={() => deleteFeedback(item)}>Xóa</button>}</div></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex-between" style={{ marginTop: '1rem' }}>
              <button className="btn btn-secondary" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Trang trước</button>
              <span>{pagination.totalCount} phản hồi · Trang {page}/{pagination.totalPages || 1}</span>
              <button className="btn btn-secondary" disabled={page >= (pagination.totalPages || 1)} onClick={() => setPage((value) => value + 1)}>Trang sau</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default MyFeedbacksPage;
