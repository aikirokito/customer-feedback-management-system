import assert from 'node:assert/strict';
import test from 'node:test';
import { formatFeedbackRating, validateFeedbackContent } from './feedbackValidation.js';

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
