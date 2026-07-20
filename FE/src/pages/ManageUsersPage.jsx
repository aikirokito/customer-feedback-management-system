import { useCallback, useEffect, useState } from 'react';
import userApi from '../api/userApi';
import { useAuth } from '../context/AuthContext';

const ROLE_OPTIONS = [
  { value: 'Customer', label: 'Khách hàng' },
  { value: 'SupportStaff', label: 'Nhân viên hỗ trợ' },
  { value: 'DepartmentManager', label: 'Quản lý phòng ban' },
  { value: 'SystemAdmin', label: 'Quản trị hệ thống' },
];

const needsDepartment = (role) => ['SupportStaff', 'DepartmentManager'].includes(role);

const ManageUsersPage = () => {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [drafts, setDrafts] = useState({});
  const [actionLoading, setActionLoading] = useState({});
  const [message, setMessage] = useState({ text: '', type: '' });

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const params = { page, pageSize: 20 };
      if (searchTerm.trim()) params.search = searchTerm.trim();
      if (roleFilter) params.role = roleFilter;
      const response = await userApi.getAllUsers(params);
      setUsers(response.data || []);
      setTotalPages(response.pagination?.totalPages || 1);
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể tải danh sách người dùng.', type: 'error' });
    } finally {
      setLoading(false);
    }
  }, [page, roleFilter, searchTerm]);

  useEffect(() => {
    Promise.all([userApi.getDepartments(), fetchUsers()])
      .then(([departmentResponse]) => setDepartments(departmentResponse.data || []))
      .catch((err) => setMessage({ text: err.normalizedMessage || 'Không thể tải dữ liệu quản trị.', type: 'error' }));
  }, [fetchUsers]);

  const getDraft = (targetUser) => drafts[targetUser.id] || {
    role: targetUser.roleName || targetUser.role,
    departmentId: targetUser.departmentId || '',
  };

  const updateDraft = (targetUser, changes) => {
    setDrafts((current) => ({
      ...current,
      [targetUser.id]: { ...getDraft(targetUser), ...changes },
    }));
  };

  const saveRole = async (targetUser) => {
    const draft = getDraft(targetUser);
    if (needsDepartment(draft.role) && !draft.departmentId) {
      setMessage({ text: 'Nhân viên hỗ trợ và quản lý phải thuộc một phòng ban.', type: 'error' });
      return;
    }

    setActionLoading((current) => ({ ...current, [`${targetUser.id}-role`]: true }));
    try {
      await userApi.updateUserRole(targetUser.id, draft.role, draft.departmentId || null);
      setMessage({ text: `Đã cập nhật vai trò cho ${targetUser.email}.`, type: 'success' });
      setDrafts((current) => {
        const next = { ...current };
        delete next[targetUser.id];
        return next;
      });
      await fetchUsers();
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể cập nhật vai trò.', type: 'error' });
    } finally {
      setActionLoading((current) => ({ ...current, [`${targetUser.id}-role`]: false }));
    }
  };

  const toggleStatus = async (targetUser) => {
    const isSelf = targetUser.id?.toLowerCase() === user?.id?.toLowerCase();
    if (isSelf) return;
    const nextActive = !targetUser.isActive;
    if (!window.confirm(`${nextActive ? 'Kích hoạt' : 'Vô hiệu hóa'} tài khoản ${targetUser.email}?`)) return;

    setActionLoading((current) => ({ ...current, [`${targetUser.id}-status`]: true }));
    try {
      await userApi.updateUserStatus(targetUser.id, nextActive);
      setMessage({ text: 'Đã cập nhật trạng thái tài khoản.', type: 'success' });
      await fetchUsers();
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể cập nhật trạng thái.', type: 'error' });
    } finally {
      setActionLoading((current) => ({ ...current, [`${targetUser.id}-status`]: false }));
    }
  };

  const deleteUser = async (targetUser) => {
    const isSelf = targetUser.id?.toLowerCase() === user?.id?.toLowerCase();
    if (isSelf || !window.confirm(`Xóa tài khoản ${targetUser.email}? Dữ liệu liên quan sẽ được giữ lại để kiểm toán.`)) return;
    setActionLoading((current) => ({ ...current, [`${targetUser.id}-delete`]: true }));
    try {
      await userApi.deleteUser(targetUser.id);
      setMessage({ text: 'Đã xóa tài khoản và thu hồi các phiên đăng nhập.', type: 'success' });
      await fetchUsers();
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể xóa tài khoản.', type: 'error' });
    } finally {
      setActionLoading((current) => ({ ...current, [`${targetUser.id}-delete`]: false }));
    }
  };

  return (
    <div className="manage-users-page">
      <div className="page-header">
        <h1>Quản lý người dùng</h1>
        <p>Cập nhật vai trò, phòng ban và trạng thái tài khoản.</p>
      </div>

      {message.text && (
        <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>
          {message.text}
        </div>
      )}

      <div className="card mb-4">
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <div className="form-group" style={{ flex: 2, minWidth: 220 }}>
            <label className="form-label" htmlFor="user-search">Tìm theo tên, email hoặc số điện thoại</label>
            <input
              id="user-search"
              className="form-control"
              value={searchTerm}
              onChange={(event) => { setSearchTerm(event.target.value); setPage(1); }}
            />
          </div>
          <div className="form-group" style={{ flex: 1, minWidth: 200 }}>
            <label className="form-label" htmlFor="user-role-filter">Vai trò</label>
            <select
              id="user-role-filter"
              className="form-control"
              value={roleFilter}
              onChange={(event) => { setRoleFilter(event.target.value); setPage(1); }}
            >
              <option value="">Tất cả</option>
              {ROLE_OPTIONS.map((role) => <option key={role.value} value={role.value}>{role.label}</option>)}
            </select>
          </div>
        </div>
      </div>

      <div className="card">
        {loading ? <div className="loading-screen"><div className="spinner" /></div> : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Người dùng</th>
                    <th>Vai trò</th>
                    <th>Phòng ban</th>
                    <th>Trạng thái</th>
                    <th>Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((targetUser) => {
                    const isSelf = targetUser.id?.toLowerCase() === user?.id?.toLowerCase();
                    const draft = getDraft(targetUser);
                    return (
                      <tr key={targetUser.id}>
                        <td>
                          <div className="font-semibold">{targetUser.fullName || targetUser.email}</div>
                          <div className="text-muted">{targetUser.email}</div>
                        </td>
                        <td>
                          <select
                            className="form-control"
                            value={draft.role}
                            disabled={isSelf}
                            onChange={(event) => updateDraft(targetUser, {
                              role: event.target.value,
                              departmentId: needsDepartment(event.target.value) ? draft.departmentId : '',
                            })}
                          >
                            {ROLE_OPTIONS.map((role) => <option key={role.value} value={role.value}>{role.label}</option>)}
                          </select>
                        </td>
                        <td>
                          <select
                            className="form-control"
                            value={draft.departmentId}
                            disabled={isSelf || !needsDepartment(draft.role)}
                            onChange={(event) => updateDraft(targetUser, { departmentId: event.target.value })}
                          >
                            <option value="">Chọn phòng ban</option>
                            {departments.map((department) => (
                              <option key={department.id} value={department.id}>{department.name}</option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <span className={`badge ${targetUser.isActive ? 'badge-success' : 'badge-gray'}`}>
                            {targetUser.isActive ? 'Hoạt động' : 'Vô hiệu hóa'}
                          </span>
                        </td>
                        <td>
                          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                            <button
                              className="btn btn-sm btn-primary"
                              disabled={isSelf || actionLoading[`${targetUser.id}-role`]}
                              onClick={() => saveRole(targetUser)}
                            >
                              Lưu
                            </button>
                            <button
                              className={`btn btn-sm ${targetUser.isActive ? 'btn-danger' : 'btn-success'}`}
                              disabled={isSelf || actionLoading[`${targetUser.id}-status`]}
                              onClick={() => toggleStatus(targetUser)}
                            >
                              {targetUser.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                            </button>
                            <button
                              className="btn btn-sm btn-danger"
                              disabled={isSelf || actionLoading[`${targetUser.id}-delete`]}
                              onClick={() => deleteUser(targetUser)}
                            >
                              Xóa
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="flex-between" style={{ marginTop: '1rem' }}>
              <button className="btn btn-secondary" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Trang trước</button>
              <span>Trang {page}/{totalPages}</span>
              <button className="btn btn-secondary" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>Trang sau</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default ManageUsersPage;
