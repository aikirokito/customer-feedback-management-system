import { Link } from 'react-router-dom';

const NotFoundPage = () => {
  return (
    <div className="empty-state" style={{ height: '80vh', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
      <div className="empty-icon" style={{ fontSize: '6rem', opacity: 0.8 }}>🛸</div>
      <h1 style={{ fontSize: '4rem', fontWeight: 900, color: 'var(--primary)', marginBottom: '8px' }}>404</h1>
      <h2 style={{ fontSize: '1.5rem', color: 'var(--text-primary)', marginBottom: '16px' }}>Không Tìm Thấy Trang</h2>
      <p style={{ fontSize: '1.1rem', marginBottom: '32px' }}>Trang bạn đang tìm kiếm có thể đã bị xóa hoặc không tồn tại.</p>
      <div>
        <Link to="/" className="btn btn-primary btn-lg">Về Trang Chủ</Link>
      </div>
    </div>
  );
};

export default NotFoundPage;
