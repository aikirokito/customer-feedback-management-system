import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import userApi from '../api/userApi';

const NotificationsPage = () => {
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const response = await userApi.getNotifications();
      setNotifications(response.data || []);
      setError('');
    } catch (requestError) {
      setError(requestError.normalizedMessage || 'Không thể tải thông báo.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void loadNotifications(); }, [loadNotifications]);

  const markRead = async (notification) => {
    if (!notification.isRead) {
      await userApi.markNotificationRead(notification.id);
      setNotifications((items) => items.map((item) => item.id === notification.id
        ? { ...item, isRead: true, readAtUtc: new Date().toISOString() }
        : item));
    }
  };

  const markAllRead = async () => {
    await userApi.markAllRead();
    setNotifications((items) => items.map((item) => ({ ...item, isRead: true })));
  };

  return (
    <div>
      <div className="page-header flex-between">
        <div><h1>Thông báo</h1><p>Theo dõi các cập nhật mới nhất của phiếu phản hồi.</p></div>
        <button className="btn btn-secondary" onClick={markAllRead} disabled={!notifications.some((item) => !item.isRead)}>Đánh dấu tất cả đã đọc</button>
      </div>
      {error && <div className="alert alert-error mb-4">{error}</div>}
      <div className="card">
        {loading ? <div className="loading-screen"><div className="spinner" /></div> : notifications.length === 0 ? (
          <div className="empty-state"><h3>Chưa có thông báo</h3><p>Các thay đổi liên quan đến bạn sẽ xuất hiện tại đây.</p></div>
        ) : notifications.map((notification) => {
          const content = (
            <div style={{ padding: '1rem', borderBottom: '1px solid var(--border)', background: notification.isRead ? 'transparent' : 'var(--primary-glow)' }}>
              <div className="flex-between" style={{ alignItems: 'flex-start', gap: '1rem' }}>
                <div>
                  <div className="font-semibold">{notification.title}</div>
                  <p className="text-muted">{notification.message}</p>
                </div>
                {!notification.isRead && <span className="badge badge-primary">Mới</span>}
              </div>
              <small className="text-muted">{new Date(notification.createdAtUtc).toLocaleString('vi-VN')}</small>
            </div>
          );
          return notification.relatedEntityType === 'Feedback' && notification.relatedEntityId ? (
            <Link key={notification.id} to={`/feedbacks/${notification.relatedEntityId}`} onClick={() => markRead(notification)}>{content}</Link>
          ) : <button key={notification.id} type="button" style={{ display: 'block', width: '100%', color: 'inherit', background: 'none', textAlign: 'left' }} onClick={() => markRead(notification)}>{content}</button>;
        })}
      </div>
    </div>
  );
};

export default NotificationsPage;
