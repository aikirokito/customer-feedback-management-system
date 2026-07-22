import assert from 'node:assert/strict';
import test from 'node:test';
import {
  canCustomerEditFeedback,
  createFeedbackEditForm,
  formatFeedbackRating,
  validateFeedbackContent,
} from './feedbackValidation.js';

const validTitle = 'Title';
const validDescription = 'D'.repeat(10);

test('enforces trimmed title boundaries', () => {
  const cases = [
    [4, false],
    [5, true],
    [200, true],
    [201, false],
  ];

  cases.forEach(([length, valid]) => {
    const { errors } = validateFeedbackContent({
      title: 'T'.repeat(length),
      description: validDescription,
    });
    assert.equal(!errors.title, valid);
  });
});

test('enforces trimmed description boundaries', () => {
  const cases = [
    [9, false],
    [10, true],
    [2000, true],
    [2001, false],
  ];

  cases.forEach(([length, valid]) => {
    const { errors } = validateFeedbackContent({
      title: validTitle,
      description: 'D'.repeat(length),
    });
    assert.equal(!errors.description, valid);
  });
});

test('rejects whitespace-only values and validates trimmed lengths', () => {
  const whitespaceOnly = validateFeedbackContent({ title: ' \t ', description: ' \r\n ' });
  assert.ok(whitespaceOnly.errors.title);
  assert.ok(whitespaceOnly.errors.description);

  const paddedValid = validateFeedbackContent({
    title: `  ${'T'.repeat(5)}  `,
    description: `  ${'D'.repeat(2000)}  `,
  });
  assert.deepEqual(paddedValid.errors, {});
  assert.equal(paddedValid.values.title.length, 5);
  assert.equal(paddedValid.values.description.length, 2000);

  const paddedInvalid = validateFeedbackContent({ title: '  Test  ', description: `  ${'D'.repeat(9)}  ` });
  assert.ok(paddedInvalid.errors.title);
  assert.ok(paddedInvalid.errors.description);
});

test('missing rating blocks submission validation', () => {
  const result = validateFeedbackContent({
    title: validTitle,
    description: validDescription,
    rating: '',
  }, { requireRating: true });

  assert.ok(result.errors.rating);
});

test('submission ratings 1 and 5 are normalized to integers', () => {
  ['1', '5'].forEach((rating) => {
    const result = validateFeedbackContent({
      title: validTitle,
      description: validDescription,
      rating,
    }, { requireRating: true });

    assert.equal(result.errors.rating, undefined);
    assert.equal(result.values.rating, Number(rating));
    assert.equal(Number.isInteger(result.values.rating), true);
  });
});

test('detail rating formatter displays saved and historical null ratings safely', () => {
  assert.equal(formatFeedbackRating(5), '5/5');
  assert.equal(formatFeedbackRating(null), 'Chưa đánh giá');
});

test('edit is available only to the owner of submitted feedback', () => {
  const owner = { id: 'customer-1', role: 'Customer' };
  const submitted = { submittedByUserId: owner.id, status: 'Submitted' };

  assert.equal(canCustomerEditFeedback(owner, submitted), true);
  assert.equal(canCustomerEditFeedback({ id: 'customer-2', role: 'Customer' }, submitted), false);
  assert.equal(canCustomerEditFeedback({ id: owner.id, role: 'SupportStaff' }, submitted), false);
});

test('edit is hidden for every non-submitted workflow status', () => {
  const owner = { id: 'customer-1', role: 'Customer' };

  ['Assigned', 'InProgress', 'Resolved', 'Closed', 'Cancelled'].forEach((status) => {
    assert.equal(canCustomerEditFeedback(owner, { submittedByUserId: owner.id, status }), false);
  });
});

test('edit form is populated with the current editable values', () => {
  assert.deepEqual(createFeedbackEditForm({
    title: 'Current title',
    description: 'Current description',
    categoryId: 'category-1',
    rating: 4,
  }), {
    title: 'Current title',
    description: 'Current description',
    categoryId: 'category-1',
    rating: '4',
  });
});

test('invalid edit validation blocks a request payload from being prepared', () => {
  const result = validateFeedbackContent({
    title: 'Test',
    description: 'Too short',
    rating: '',
  }, { requireRating: true });

  assert.ok(Object.keys(result.errors).length > 0);
  assert.ok(result.errors.title);
  assert.ok(result.errors.description);
  assert.ok(result.errors.rating);
});

test('valid edit validation provides an integer rating for the update payload', () => {
  const result = validateFeedbackContent({
    title: 'Updated title',
    description: 'Updated description',
    rating: '5',
  }, { requireRating: true });

  assert.deepEqual(result.errors, {});
  assert.equal(result.values.rating, 5);
  assert.equal(Number.isInteger(result.values.rating), true);
});

test('refreshed feedback values populate the detail edit model after a successful update', () => {
  const updated = createFeedbackEditForm({
    title: 'Changed title',
    description: 'Changed description',
    categoryId: 'category-2',
    rating: 1,
  });

  assert.equal(updated.title, 'Changed title');
  assert.equal(updated.description, 'Changed description');
  assert.equal(updated.categoryId, 'category-2');
  assert.equal(updated.rating, '1');
});
