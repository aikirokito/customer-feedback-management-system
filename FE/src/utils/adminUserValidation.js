export const ADMIN_CREATABLE_ROLE_OPTIONS = [
  { value: 'SupportStaff', label: 'Nhân viên hỗ trợ' },
  { value: 'DepartmentManager', label: 'Quản lý' },
];

const CREATABLE_ROLES = new Set(ADMIN_CREATABLE_ROLE_OPTIONS.map(({ value }) => value));
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE_PATTERN = /^\+?[0-9\s\-()]{7,20}$/;
const PASSWORD_SPECIAL_CHARACTERS = `!@#$%^&*()_+-=[]{};':"\\|,.<>/?`;

export const validateAdminUser = (form) => {
  const values = {
    email: form.email?.trim().toLowerCase() || '',
    password: form.password || '',
    confirmPassword: form.confirmPassword || '',
    firstName: form.firstName?.trim() || '',
    lastName: form.lastName?.trim() || '',
    phoneNumber: form.phoneNumber?.trim() || null,
    role: form.role || '',
  };
  const errors = {};

  if (!values.firstName) errors.firstName = 'Tên là bắt buộc.';
  else if (values.firstName.length > 100) errors.firstName = 'Tên không được vượt quá 100 ký tự.';

  if (!values.lastName) errors.lastName = 'Họ là bắt buộc.';
  else if (values.lastName.length > 100) errors.lastName = 'Họ không được vượt quá 100 ký tự.';

  if (!values.email) errors.email = 'Email là bắt buộc.';
  else if (values.email.length > 256 || !EMAIL_PATTERN.test(values.email)) {
    errors.email = 'Vui lòng nhập email hợp lệ.';
  }

  if (!values.password) errors.password = 'Mật khẩu là bắt buộc.';
  else if (values.password.length < 8 || values.password.length > 128 ||
    !/[A-Z]/.test(values.password) || !/[a-z]/.test(values.password) ||
    !/\d/.test(values.password) ||
    ![...values.password].some((character) => PASSWORD_SPECIAL_CHARACTERS.includes(character))) {
    errors.password = 'Mật khẩu phải dài 8–128 ký tự và có chữ hoa, chữ thường, số, ký tự đặc biệt.';
  }

  if (!values.confirmPassword) errors.confirmPassword = 'Xác nhận mật khẩu là bắt buộc.';
  else if (values.confirmPassword !== values.password) errors.confirmPassword = 'Mật khẩu xác nhận không khớp.';

  if (!CREATABLE_ROLES.has(values.role)) errors.role = 'Chỉ có thể tạo tài khoản Staff hoặc Manager.';
  if (values.phoneNumber && !PHONE_PATTERN.test(values.phoneNumber)) {
    errors.phoneNumber = 'Số điện thoại không hợp lệ.';
  }

  return { values, errors };
};
