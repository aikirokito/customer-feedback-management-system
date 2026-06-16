import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import feedbackApi, { FEEDBACK_CATEGORIES } from '../api/feedbackApi';

const SubmitFeedbackPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    title: '',
    description: '',
    category: FEEDBACK_CATEGORIES[0].value,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.title.trim() || !form.description.trim() || !form.category) {
      setError('Vui lòng điền đầy đủ thông tin.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      await feedbackApi.submitFeedback({
        title: form.title.trim(),
        description: form.description.trim(),
        category: form.category,
      });
      navigate('/my-feedbacks', { state: { successMsg: 'Gửi phản hồi thành công!' } });
    } catch (err) {
      setError(err.normalizedMessage || 'Có lỗi xảy ra, vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="submit-feedback-page">
      <div className="page-header">
        <h1>Gửi Phản Hồi Mới</h1>
        <p>Hãy chia sẻ ý kiến hoặc vấn đề của bạn, chúng tôi luôn lắng nghe.</p>
      </div>

      <div className="card" style={{ maxWidth: 600 }}>
        {error && <div className="alert alert-error mb-4">⚠️ {error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="feedback-title">Tiêu đề</label>
            <input
              id="feedback-title"
              name="title"
              type="text"
              className="form-control"
              placeholder="Tóm tắt vấn đề của bạn..."
              value={form.title}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-category">Danh mục</label>
            <select id="feedback-category" name="category" className="form-control" value={form.category} onChange={handleChange}>
              {FEEDBACK_CATEGORIES.map(c => (
                <option key={c.value} value={c.value}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-desc">Nội dung chi tiết</label>
            <textarea
              id="feedback-desc"
              name="description"
              className="form-control"
              placeholder="Mô tả chi tiết vấn đề bạn đang gặp phải..."
              value={form.description}
              onChange={handleChange}
            />
          </div>

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Đang gửi...' : '📤 Gửi phản hồi'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default SubmitFeedbackPage;
