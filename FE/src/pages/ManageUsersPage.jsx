import { useState, useEffect, useCallback } from 'react';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';

const ROLE_OPTIONS = [
  { value: 'Customer', label: 'Khách hàng' },
  { value: 'SupportStaff', label: 'Nhân viên hỗ trợ' },
  { value: 'DepartmentManager', label: 'Trưởng bộ phận' },
  { value: 'SystemAdmin', label: 'Quản trị hệ thống' },
];

const ManageUsersPage = () => {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  
  // Temporary role selection state per user
  const [selectedRoles, setSelectedRoles] = useState({});
  const [actionLoading, setActionLoading] = useState({}); // { [userId_actionType]: boolean }
  
  // Alert message state
  const [message, setMessage] = useState({ text: '', type: '' });

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const params = {};
      if (searchTerm.trim()) params.search = searchTerm.trim();
      if (roleFilter) params.role = roleFilter;
      
      const res = await userApi.getAllUsers(params);
      setUsers(res.data || []);
    } catch (err) {
      console.error(err);
      showAlert('Không thể tải danh sách người dùng.', 'error');
    } finally {
      setLoading(false);
    }
  }, [searchTerm, roleFilter]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const showAlert = (text, type = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage({ text: '', type: '' }), 4000);
  };

  const handleRoleChangeLocal = (userId, newRole) => {
    setSelectedRoles(prev => ({
      ...prev,
      [userId]: newRole
    }));
  };

  const handleSaveRole = async (userId, targetUser) => {
    const nextRole = selectedRoles[userId];
    if (!nextRole || nextRole === (targetUser.roleName || targetUser.role)) return;

    setActionLoading(prev => ({ ...prev, [`${userId}_role`]: true }));
    try {
      await userApi.updateUserRole(userId, nextRole);
      showAlert(`Đã cập nhật vai trò cho người dùng ${targetUser.email} thành ${nextRole}.`);
      
      // Clear local selection state for this user
      setSelectedRoles(prev => {
        const copy = { ...prev };
        delete copy[userId];
        return copy;
      });
      
      fetchUsers();
    } catch (err) {
      showAlert('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setActionLoading(prev => ({ ...prev, [`${userId}_role`]: false }));
    }
  };

  const handleToggleStatus = async (targetUser) => {
    const isSelf = targetUser.id?.toLowerCase() === user?.id?.toLowerCase();
    if (isSelf) {
      showAlert('Bạn không thể vô hiệu hóa tài khoản của chính mình!', 'error');
      return;
    }

    const nextActive = !targetUser.isActive;
    setActionLoading(prev => ({ ...prev, [`${targetUser.id}_status`]: true }));
    try {
      await userApi.updateUserStatus(targetUser.id, nextActive);
      showAlert(`Đã ${nextActive ? 'kích hoạt' : 'vô hiệu hóa'} tài khoản ${targetUser.email}.`);
      fetchUsers();
    } catch (err) {
      showAlert('Lỗi: ' + (err.normalizedMessage || err.message), 'error');
    } finally {
      setActionLoading(prev => ({ ...prev, [`${targetUser.id}_status`]: false }));
    }
  };

  return (
    <div className="manage-users-page">
      <div className="page-header">
        <h1>Quản Lý Người Dùng</h1>
        <p>Quản trị viên có thể xem, thay đổi vai trò và kích hoạt/vô hiệu hóa tài khoản người dùng.</p>
      </div>

      {message.text && (
        <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>
          {message.text}
        </div>
      )}

      {/* Search & Filters */}
      <div className="card mb-4">
        <div className="card-header">
          <h3 className="card-title">Bộ lọc tìm kiếm</h3>
        </div>
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', padding: '0 1.5rem 1.5rem' }}>
          <div className="form-group" style={{ flex: 2, minWidth: 200, marginBottom: 0 }}>
            <label className="form-label">Tìm theo Tên hoặc Email</label>
            <input
              id="filter-search-user"
              type="text"
              className="form-control"
              placeholder="Nhập tên hoặc email..."
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
            />
          </div>
          <div className="form-group" style={{ flex: 1, minWidth: 180, marginBottom: 0 }}>
            <label className="form-label">Vai trò</label>
            <select
              id="filter-role-user"
              className="form-control"
              value={roleFilter}
              onChange={e => setRoleFilter(e.target.value)}
            >
              <option value="">Tất cả</option>
              <option value="Customer">Khách hàng (Customer)</option>
              <option value="SupportStaff">Nhân viên hỗ trợ (SupportStaff)</option>
              <option value="DepartmentManager">Trưởng bộ phận (DepartmentManager)</option>
              <option value="SystemAdmin">Quản trị hệ thống (SystemAdmin)</option>
            </select>
          </div>
        </div>
      </div>

      {/* Users Table */}
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
                  <th>Hành Động Vai Trò</th>
                  <th>Ngày Tạo</th>
                  <th>Trạng Thái</th>
                </tr>
              </thead>
              <tbody>
                {users.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="text-center py-4">Không tìm thấy người dùng nào phù hợp.</td>
                  </tr>
                ) : (
                  users.map(u => {
                    const isSelf = u.id?.toLowerCase() === user?.id?.toLowerCase();
                    const activeRole = selectedRoles[u.id] !== undefined ? selectedRoles[u.id] : (u.roleName || u.role);
                    const isRoleChanged = activeRole !== (u.roleName || u.role);
                    
                    return (
                      <tr key={u.id}>
                        <td className="font-semibold text-muted" style={{ fontSize: '0.8rem' }}>
                          {u.id?.substring?.(0, 8) || u.id}
                        </td>
                        <td className="font-semibold">
                          {u.fullName || `${u.firstName || ''} ${u.lastName || ''}`.trim() || '---'}
                          {isSelf && <span className="text-primary" style={{ marginLeft: '0.5rem', fontSize: '0.8rem' }}>(Bạn)</span>}
                        </td>
                        <td>{u.email}</td>
                        <td>
                          <select
                            id={`role-select-${u.id}`}
                            className="form-control"
                            style={{ padding: '6px 12px', fontSize: '0.85rem', maxWidth: 180 }}
                            value={activeRole}
                            disabled={isSelf}
                            onChange={e => handleRoleChangeLocal(u.id, e.target.value)}
                          >
                            {ROLE_OPTIONS.map(opt => (
                              <option key={opt.value} value={opt.value}>
                                {opt.label}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <button
                            id={`btn-save-role-${u.id}`}
                            className="btn btn-sm btn-primary"
                            disabled={!isRoleChanged || actionLoading[`${u.id}_role`] || isSelf}
                            onClick={() => handleSaveRole(u.id, u)}
                          >
                            {actionLoading[`${u.id}_role`] ? 'Đang lưu...' : 'Lưu'}
                          </button>
                        </td>
                        <td>
                          {u.createdAtUtc 
                            ? new Date(u.createdAtUtc).toLocaleDateString('vi-VN') 
                            : u.createdAt 
                              ? new Date(u.createdAt).toLocaleDateString('vi-VN') 
                              : '---'}
                        </td>
                        <td>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                            <span className={`badge ${u.isActive !== false ? 'badge-success' : 'badge-gray'}`}>
                              {u.isActive !== false ? 'Hoạt động' : 'Vô hiệu'}
                            </span>
                            <button
                              id={`btn-toggle-status-${u.id}`}
                              className={`btn btn-sm ${u.isActive !== false ? 'btn-danger' : 'btn-success'}`}
                              disabled={isSelf || actionLoading[`${u.id}_status`]}
                              onClick={() => handleToggleStatus(u)}
                              style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                            >
                              {actionLoading[`${u.id}_status`] 
                                ? '...' 
                                : (u.isActive !== false ? 'Vô hiệu hóa' : 'Kích hoạt')}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default ManageUsersPage;
