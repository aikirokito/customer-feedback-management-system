import { useCallback, useEffect, useState } from 'react';
import feedbackApi from '../api/feedbackApi';
import userApi from '../api/userApi';

const emptyForm = { name: '', description: '', departmentId: '' };

const ManageCategoriesPage = () => {
  const [categories, setCategories] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState({ text: '', type: '' });

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [categoryResponse, departmentResponse] = await Promise.all([
        feedbackApi.getAllCategories(),
        userApi.getDepartments(),
      ]);
      setCategories(categoryResponse.data || []);
      setDepartments(departmentResponse.data || []);
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể tải dữ liệu danh mục.', type: 'error' });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!form.name.trim()) return;

    setSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        description: editingId ? form.description.trim() : form.description.trim() || null,
        departmentId: form.departmentId || null,
        clearDepartment: Boolean(editingId && !form.departmentId),
      };
      if (editingId) {
        await feedbackApi.updateCategory(editingId, payload);
      } else {
        await feedbackApi.createCategory(payload);
      }
      setMessage({ text: editingId ? 'Đã cập nhật danh mục.' : 'Đã tạo danh mục.', type: 'success' });
      resetForm();
      await loadData();
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể lưu danh mục.', type: 'error' });
    } finally {
      setSaving(false);
    }
  };

  const startEdit = (category) => {
    setEditingId(category.id);
    setForm({
      name: category.name,
      description: category.description || '',
      departmentId: category.departmentId || '',
    });
  };

  const toggleCategory = async (category) => {
    if (!window.confirm(`${category.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'} danh mục "${category.name}"?`)) return;
    try {
      await feedbackApi.updateCategory(category.id, { isActive: !category.isActive });
      setMessage({ text: 'Đã cập nhật trạng thái danh mục.', type: 'success' });
      await loadData();
    } catch (err) {
      setMessage({ text: err.normalizedMessage || 'Không thể cập nhật danh mục.', type: 'error' });
    }
  };

  return (
    <div className="manage-categories-page">
      <div className="page-header">
        <h1>Quản lý danh mục phản hồi</h1>
        <p>Danh mục bị vô hiệu hóa không xuất hiện trong biểu mẫu mới; phản hồi cũ vẫn giữ nguyên tham chiếu.</p>
      </div>

      {message.text && (
        <div className={`alert alert-${message.type === 'error' ? 'error' : 'success'} mb-4`}>
          {message.text}
        </div>
      )}

      <form className="card mb-4" onSubmit={handleSubmit}>
        <div className="card-header">
          <h2 className="card-title">{editingId ? 'Cập nhật danh mục' : 'Tạo danh mục'}</h2>
        </div>
        <div style={{ display: 'grid', gap: '1rem', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))' }}>
          <div className="form-group">
            <label className="form-label" htmlFor="category-name">Tên danh mục *</label>
            <input
              id="category-name"
              className="form-control"
              maxLength={100}
              required
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
            />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="category-department">Phòng ban xử lý</label>
            <select
              id="category-department"
              className="form-control"
              value={form.departmentId}
              onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value }))}
            >
              <option value="">Chưa gán phòng ban</option>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>{department.name}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="form-group">
          <label className="form-label" htmlFor="category-description">Mô tả</label>
          <textarea
            id="category-description"
            className="form-control"
            maxLength={500}
            value={form.description}
            onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
          />
        </div>
        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button className="btn btn-primary" type="submit" disabled={saving}>
            {saving ? 'Đang lưu...' : 'Lưu danh mục'}
          </button>
          {editingId && <button className="btn btn-secondary" type="button" onClick={resetForm}>Hủy</button>}
        </div>
      </form>

      <div className="card">
        {loading ? <div className="loading-screen"><div className="spinner" /></div> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Tên</th>
                  <th>Mô tả</th>
                  <th>Phòng ban</th>
                  <th>Trạng thái</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {categories.map((category) => (
                  <tr key={category.id}>
                    <td className="font-semibold">{category.name}</td>
                    <td>{category.description || '---'}</td>
                    <td>{category.departmentName || '---'}</td>
                    <td>
                      <span className={`badge ${category.isActive ? 'badge-success' : 'badge-gray'}`}>
                        {category.isActive ? 'Đang hoạt động' : 'Đã vô hiệu hóa'}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button className="btn btn-sm btn-secondary" type="button" onClick={() => startEdit(category)}>Sửa</button>
                        <button
                          className={`btn btn-sm ${category.isActive ? 'btn-danger' : 'btn-success'}`}
                          type="button"
                          onClick={() => toggleCategory(category)}
                        >
                          {category.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}
                        </button>
                      </div>
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

export default ManageCategoriesPage;
