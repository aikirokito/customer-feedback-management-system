export const FEEDBACK_LIMITS = {
  title: { min: 5, max: 200 },
  description: { min: 10, max: 2000 },
};

export const validateFeedbackContent = ({ title = '', description = '' }) => {
  const values = {
    title: title.trim(),
    description: description.trim(),
  };
  const errors = {};

  if (!values.title) {
    errors.title = 'Vui lòng nhập tiêu đề.';
  } else if (values.title.length < FEEDBACK_LIMITS.title.min) {
    errors.title = `Tiêu đề phải có ít nhất ${FEEDBACK_LIMITS.title.min} ký tự.`;
  } else if (values.title.length > FEEDBACK_LIMITS.title.max) {
    errors.title = `Tiêu đề không được vượt quá ${FEEDBACK_LIMITS.title.max} ký tự.`;
  }

  if (!values.description) {
    errors.description = 'Vui lòng nhập nội dung chi tiết.';
  } else if (values.description.length < FEEDBACK_LIMITS.description.min) {
    errors.description = `Nội dung phải có ít nhất ${FEEDBACK_LIMITS.description.min} ký tự.`;
  } else if (values.description.length > FEEDBACK_LIMITS.description.max) {
    errors.description = `Nội dung không được vượt quá ${FEEDBACK_LIMITS.description.max} ký tự.`;
  }

  return { values, errors };
};
