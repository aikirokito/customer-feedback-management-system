import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import feedbackApi from '../api/feedbackApi';
import { validateFeedbackContent } from '../utils/feedbackValidation';

const MAX_FILES = 3;
const MAX_FILE_SIZE = 5 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['jpg', 'jpeg', 'png', 'gif', 'pdf', 'docx', 'xlsx'];

const SubmitFeedbackPage = () => {
  const navigate = useNavigate();
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState({
    title: '',
    description: '',
    categoryId: '',
    rating: '',
  });
  const [files, setFiles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});

  useEffect(() => {
    feedbackApi.getCategories()
      .then((response) => {
        const activeCategories = response.data || [];
        setCategories(activeCategories);
        setForm((current) => ({
          ...current,
          categoryId: current.categoryId || activeCategories[0]?.id || '',
        }));
      })
      .catch((err) => setError(err.normalizedMessage || 'Không thể tải danh mục phản hồi.'));
  }, []);

  const handleChange = (event) => {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
    setFieldErrors((current) => ({ ...current, [event.target.name]: '' }));
    setError('');
  };

  const handleFilesChange = (event) => {
    const selectedFiles = Array.from(event.target.files || []);
    if (selectedFiles.length > MAX_FILES) {
      setError(`Chỉ được tải lên tối đa ${MAX_FILES} tệp.`);
      event.target.value = '';
      return;
    }

    const invalidFile = selectedFiles.find((file) => {
      const extension = file.name.split('.').pop()?.toLowerCase();
      return file.size > MAX_FILE_SIZE || !ALLOWED_EXTENSIONS.includes(extension);
    });

    if (invalidFile) {
      setError(`Tệp "${invalidFile.name}" không hợp lệ. Mỗi tệp tối đa 5 MB và phải đúng định dạng cho phép.`);
      event.target.value = '';
      return;
    }

    setFiles(selectedFiles);
    setError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    const validation = validateFeedbackContent(form, { requireRating: true });
    setFieldErrors(validation.errors);

    if (Object.keys(validation.errors).length > 0) {
      return;
    }

    if (!form.categoryId) {
      setError('Vui lòng điền đầy đủ các trường bắt buộc.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const response = await feedbackApi.submitFeedback({
        title: validation.values.title,
        description: validation.values.description,
        categoryId: form.categoryId,
        rating: validation.values.rating,
      });
      const feedbackId = response.data?.id;

      if (feedbackId && files.length > 0) {
        await Promise.all(files.map((file) => feedbackApi.uploadAttachment(feedbackId, file)));
      }

      navigate('/my-feedbacks', {
        state: { successMsg: 'Gửi phản hồi thành công!' },
        replace: true,
      });
    } catch (err) {
      setError(err.normalizedMessage || 'Có lỗi xảy ra, vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="submit-feedback-page">
      <div className="page-header">
        <h1>Gửi phản hồi mới</h1>
        <p>Chia sẻ vấn đề hoặc đề xuất của bạn để đội ngũ hỗ trợ xử lý.</p>
      </div>

      <div className="card" style={{ maxWidth: 720 }}>
        {error && <div className="alert alert-error mb-4">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="feedback-title">Tiêu đề *</label>
            <input
              id="feedback-title"
              name="title"
              type="text"
              className="form-control"
              required
              value={form.title}
              onChange={handleChange}
              aria-invalid={Boolean(fieldErrors.title)}
              aria-describedby={fieldErrors.title ? 'feedback-title-error' : undefined}
            />
            {fieldErrors.title && <small id="feedback-title-error" className="text-danger">{fieldErrors.title}</small>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-category">Danh mục *</label>
            <select
              id="feedback-category"
              name="categoryId"
              className="form-control"
              required
              value={form.categoryId}
              onChange={handleChange}
              disabled={categories.length === 0}
            >
              {categories.length === 0 && <option value="">Chưa có danh mục hoạt động</option>}
              {categories.map((category) => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-description">Nội dung chi tiết *</label>
            <textarea
              id="feedback-description"
              name="description"
              className="form-control"
              required
              value={form.description}
              onChange={handleChange}
              aria-invalid={Boolean(fieldErrors.description)}
              aria-describedby={fieldErrors.description ? 'feedback-description-error' : undefined}
            />
            {fieldErrors.description && <small id="feedback-description-error" className="text-danger">{fieldErrors.description}</small>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-rating">Đánh giá *</label>
            <select
              id="feedback-rating"
              name="rating"
              className="form-control"
              required
              value={form.rating}
              onChange={handleChange}
              aria-invalid={Boolean(fieldErrors.rating)}
              aria-describedby={fieldErrors.rating ? 'feedback-rating-error' : undefined}
            >
              <option value="">-- Chọn mức đánh giá --</option>
              <option value="1">1</option>
              <option value="2">2</option>
              <option value="3">3</option>
              <option value="4">4</option>
              <option value="5">5</option>
            </select>
            {fieldErrors.rating && <small id="feedback-rating-error" className="text-danger">{fieldErrors.rating}</small>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-files">Tệp đính kèm (tối đa 3 tệp)</label>
            <input
              id="feedback-files"
              type="file"
              className="form-control"
              multiple
              accept=".jpg,.jpeg,.png,.gif,.pdf,.docx,.xlsx"
              onChange={handleFilesChange}
            />
            <small className="text-muted">Mỗi tệp tối đa 5 MB.</small>
          </div>

          <button type="submit" className="btn btn-primary" disabled={loading || categories.length === 0}>
            {loading ? 'Đang gửi...' : 'Gửi phản hồi'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default SubmitFeedbackPage;
