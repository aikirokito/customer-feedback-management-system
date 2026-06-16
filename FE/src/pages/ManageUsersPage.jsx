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
      setUsers(res.data || []);
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
                  <th>Họ &amp; Tên</th>
                  <th>Email</th>
                  <th>Vai Trò</th>
                  <th>Ngày Tạo</th>
                  <th>Trạng Thái</th>
                </tr>
              </thead>
              <tbody>
                {users.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-4">Không có dữ liệu người dùng.</td></tr>
                ) : users.map(u => (
                  <tr key={u.id}>
                    <td>{u.id}</td>
                    <td className="font-semibold">{u.fullName || `${u.firstName || ''} ${u.lastName || ''}`.trim() || '---'}</td>
                    <td>{u.email}</td>
                    <td><span className="badge badge-primary">{u.roleName || u.role}</span></td>
                    <td>{u.createdAtUtc ? new Date(u.createdAtUtc).toLocaleDateString('vi-VN') : u.createdAt ? new Date(u.createdAt).toLocaleDateString('vi-VN') : '---'}</td>
                    <td>
                      <span className={`badge ${u.isActive !== false ? 'badge-success' : 'badge-gray'}`}>
                        {u.isActive !== false ? 'Hoạt động' : 'Vô hiệu'}
                      </span>
                    </td>
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
