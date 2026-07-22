import assert from 'node:assert/strict';
import test from 'node:test';
import { validateAdminUser } from './adminUserValidation.js';

const validForm = (role = 'SupportStaff') => ({
  email: '  NEW.STAFF@example.com  ',
  password: 'Password1!',
  confirmPassword: 'Password1!',
  firstName: '  New  ',
  lastName: '  Staff  ',
  phoneNumber: '',
  role,
});

test('valid Staff and Manager forms produce trimmed API values', () => {
  for (const role of ['SupportStaff', 'DepartmentManager']) {
    const { values, errors } = validateAdminUser(validForm(role));
    assert.deepEqual(errors, {});
    assert.equal(values.email, 'new.staff@example.com');
    assert.equal(values.firstName, 'New');
    assert.equal(values.lastName, 'Staff');
    assert.equal(values.role, role);
    assert.equal(values.phoneNumber, null);
  }
});

test('missing required fields are rejected before submission', () => {
  const { errors } = validateAdminUser({});

  assert.deepEqual(Object.keys(errors).sort(), [
    'confirmPassword', 'email', 'firstName', 'lastName', 'password', 'role',
  ]);
});

test('invalid email and unsupported roles are rejected', () => {
  const form = validForm('SystemAdmin');
  form.email = 'invalid-email';

  const { errors } = validateAdminUser(form);

  assert.match(errors.email, /email hợp lệ/);
  assert.match(errors.role, /Staff hoặc Manager/);
});
