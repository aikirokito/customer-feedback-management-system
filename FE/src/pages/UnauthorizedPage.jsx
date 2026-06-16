import { Link } from 'react-router-dom';

const UnauthorizedPage = () => {
  return (
    <div className="empty-state" style={{ height: '80vh', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
      <div className="empty-icon" style={{ fontSize: '5rem', color: 'var(--danger)' }}>🚫</div>
      <h1 style={{ fontSize: '2rem', color: 'var(--text-primary)', marginBottom: '16px' }}>Truy Cập Bị Từ Chối</h1>
      <p style={{ fontSize: '1.1rem', marginBottom: '32px' }}>Bạn không có quyền truy cập vào trang này.</p>
      <div>
        <Link to="/" className="btn btn-primary">Quay Về Trang Chủ</Link>
      </div>
    </div>
  );
};

export default UnauthorizedPage;
