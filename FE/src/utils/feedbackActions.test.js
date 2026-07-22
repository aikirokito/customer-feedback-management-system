import assert from 'node:assert/strict';
import test from 'node:test';
import { getAssignedListActionLabel, getFeedbackActionPolicy } from './feedbackActions.js';

const feedbackFor = (user, status) => ({
  status,
  submittedByUserId: user.role === 'Customer' ? user.id : 'customer-1',
  assignedToUserId: user.role === 'SupportStaff' ? user.id : 'staff-1',
});

test('Customer owner can edit and cancel only Submitted feedback', () => {
  const customer = { id: 'customer-1', role: 'Customer' };
  const submitted = getFeedbackActionPolicy(customer, feedbackFor(customer, 'Submitted'));
  assert.equal(submitted.canEdit, true);
  assert.equal(submitted.canCancel, true);

  ['Assigned', 'InProgress', 'Resolved', 'Closed', 'Cancelled'].forEach((status) => {
    const policy = getFeedbackActionPolicy(customer, feedbackFor(customer, status));
    assert.equal(policy.canEdit, false);
    assert.equal(policy.canCancel, false);
  });
});

test('Staff sees only status-appropriate actions while assigned', () => {
  const staff = { id: 'staff-1', role: 'SupportStaff' };
  const assigned = getFeedbackActionPolicy(staff, feedbackFor(staff, 'Assigned'));
  assert.equal(assigned.canStart, true);
  assert.equal(assigned.canRespond, true);
  assert.equal(assigned.canResolve, false);

  const inProgress = getFeedbackActionPolicy(staff, feedbackFor(staff, 'InProgress'));
  assert.equal(inProgress.canStart, false);
  assert.equal(inProgress.canRespond, true);
  assert.equal(inProgress.canResolve, true);

  ['Resolved', 'Closed', 'Cancelled'].forEach((status) => {
    const policy = getFeedbackActionPolicy(staff, feedbackFor(staff, status));
    assert.equal(policy.hasManagementActions, false);
    assert.equal(policy.canRespond, false);
  });
});

test('Manager sees assignment, reassignment, or close only at the matching status', () => {
  const manager = { id: 'manager-1', role: 'DepartmentManager' };
  const expected = {
    Submitted: ['canAssign'],
    Assigned: ['canReassign'],
    InProgress: ['canReassign'],
    Resolved: ['canClose'],
    Closed: [],
    Cancelled: [],
  };

  Object.entries(expected).forEach(([status, allowed]) => {
    const policy = getFeedbackActionPolicy(manager, feedbackFor(manager, status));
    ['canAssign', 'canReassign', 'canClose'].forEach((action) => {
      assert.equal(policy[action], allowed.includes(action), `${status} ${action}`);
    });
  });
});

test('Admin receives no operational feedback actions', () => {
  const admin = { id: 'admin-1', role: 'SystemAdmin' };
  ['Submitted', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Cancelled'].forEach((status) => {
    const policy = getFeedbackActionPolicy(admin, feedbackFor(admin, status));
    assert.equal(policy.hasManagementActions, false);
    assert.equal(policy.canRespond, false);
  });
});

test('Historical Staff list rows use a view label instead of a processing label', () => {
  assert.equal(getAssignedListActionLabel('SupportStaff', 'Assigned'), 'Xử lý');
  assert.equal(getAssignedListActionLabel('SupportStaff', 'InProgress'), 'Xử lý');
  assert.equal(getAssignedListActionLabel('SupportStaff', 'Resolved'), 'Chi tiết');
  assert.equal(getAssignedListActionLabel('SupportStaff', 'Closed'), 'Chi tiết');
  assert.equal(getAssignedListActionLabel('SupportStaff', 'Cancelled'), 'Chi tiết');
});
