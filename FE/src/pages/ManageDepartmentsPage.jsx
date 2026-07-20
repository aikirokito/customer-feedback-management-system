import { useCallback, useEffect, useState } from 'react';
import userApi from '../api/userApi';

const emptyForm = { name: '', description: '' };

const ManageDepartmentsPage = () => {
  const [departments, setDepartments] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState({ text: '', type: '' });

  const loadDepartments = useCallback(async () => {
    setLoading(true);
    try {
      const response = await userApi.getAllDepartments();
      setDepartments(response.data || []);
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể tải danh sách phòng ban.', type: 'error' });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void loadDepartments(); }, [loadDepartments]);

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    try {
      const payload = { name: form.name.trim(), description: form.description.trim() || null };
      if (editingId) {
        await userApi.updateDepartment(editingId, { ...payload, clearDescription: !payload.description });
      } else {
        await userApi.createDepartment(payload);
      }
      setMessage({ text: editingId ? 'Đã cập nhật phòng ban.' : 'Đã tạo phòng ban.', type: 'success' });
      setForm(emptyForm);
      setEditingId(null);
      await loadDepartments();
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể lưu phòng ban.', type: 'error' });
    } finally {
      setSaving(false);
    }
  };

  const edit = (department) => {
    setEditingId(department.id);
    setForm({ name: department.name, description: department.description || '' });
  };

  const toggle = async (department) => {
    try {
      await userApi.updateDepartment(department.id, { isActive: !department.isActive });
      await loadDepartments();
    } catch (error) {
      setMessage({ text: error.normalizedMessage || 'Không thể đổi trạng thái phòng ban.', type: 'error' });
    }
  };

  return (
    <div>
      <div className="page-header"><h1>Quản lý phòng ban</h1><p>Tổ chức đội ngũ hỗ trợ và phân luồng danh mục phản hồi.</p></div>
      {message.text && <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>{message.text}</div>}
      <form className="card mb-4" onSubmit={submit}>
        <div className="card-header"><h2 className="card-title">{editingId ? 'Cập nhật phòng ban' : 'Tạo phòng ban'}</h2></div>
        <div style={{ display: 'grid', gridTemplateColumns: 'minmax(220px, 1fr) minmax(280px, 2fr)', gap: '1rem' }}>
          <div className="form-group"><label className="form-label">Tên phòng ban</label><input className="form-control" maxLength={100} required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
          <div className="form-group"><label className="form-label">Mô tả</label><input className="form-control" maxLength={500} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
        </div>
        <div className="flex gap-2"><button className="btn btn-primary" disabled={saving}>{saving ? 'Đang lưu...' : 'Lưu'}</button>{editingId && <button type="button" className="btn btn-secondary" onClick={() => { setEditingId(null); setForm(emptyForm); }}>Hủy</button>}</div>
      </form>
      <div className="card">
        {loading ? <div className="loading-screen"><div className="spinner" /></div> : (
          <div className="table-wrap"><table><thead><tr><th>Tên</th><th>Mô tả</th><th>Trạng thái</th><th>Hành động</th></tr></thead><tbody>
            {departments.map((department) => <tr key={department.id}><td className="font-semibold">{department.name}</td><td>{department.description || '---'}</td><td><span className={`badge ${department.isActive ? 'badge-success' : 'badge-gray'}`}>{department.isActive ? 'Hoạt động' : 'Vô hiệu hóa'}</span></td><td><div className="flex gap-2"><button className="btn btn-sm btn-secondary" onClick={() => edit(department)}>Sửa</button><button className={`btn btn-sm ${department.isActive ? 'btn-danger' : 'btn-success'}`} onClick={() => toggle(department)}>{department.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}</button></div></td></tr>)}
          </tbody></table></div>
        )}
      </div>
    </div>
  );
};

export default ManageDepartmentsPage;
