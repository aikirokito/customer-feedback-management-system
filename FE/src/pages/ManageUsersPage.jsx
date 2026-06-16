import { useState, useEffect } from 'react';
import userApi from '../api/userApi';

const ManageUsersPage = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      const res = await userApi.getAllUsers();
      setUsers(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="manage-users-page">
      <div className="page-header">
        <h1>Quản Lý Người Dùng</h1>
        <p>Quản trị viên có thể xem và phân quyền người dùng tại đây.</p>
      </div>

      <div className="card">
        {loading ? (
          <div className="loading-screen" style={{ height: 200 }}><div className="spinner" /></div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Họ & Tên</th>
                  <th>Email</th>
                  <th>Vai Trò</th>
                  <th>Ngày Tạo</th>
                  <th>Trạng Thái</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id}>
                    <td>{u.id}</td>
                    <td className="font-semibold">{u.fullName || '---'}</td>
                    <td>{u.email}</td>
                    <td><span className="badge badge-primary">{u.role}</span></td>
                    <td>{new Date(u.createdAt).toLocaleDateString('vi-VN')}</td>
                    <td><span className="badge badge-success">Hoạt động</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default ManageUsersPage;
