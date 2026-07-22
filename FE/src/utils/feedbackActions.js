const STAFF_ACTIONABLE_STATUSES = new Set(['Assigned', 'InProgress']);
const MANAGER_ASSIGNABLE_STATUSES = new Set(['Submitted', 'Assigned', 'InProgress']);

export const getFeedbackActionPolicy = (user, feedback) => {
  const role = user?.role;
  const status = feedback?.status;
  const isOwner = role === 'Customer'
    && Boolean(user?.id && feedback?.submittedByUserId)
    && user.id === feedback.submittedByUserId;
  const isAssignedStaff = role === 'SupportStaff'
    && Boolean(user?.id && feedback?.assignedToUserId)
    && user.id === feedback.assignedToUserId;
  const isManager = role === 'DepartmentManager';

  return {
    canEdit: isOwner && status === 'Submitted',
    canCancel: isOwner && status === 'Submitted',
    canComment: isOwner && status === 'Submitted',
    canStart: isAssignedStaff && status === 'Assigned',
    canRespond: isAssignedStaff && STAFF_ACTIONABLE_STATUSES.has(status),
    canResolve: isAssignedStaff && status === 'InProgress',
    canAssign: isManager && status === 'Submitted',
    canReassign: isManager && (status === 'Assigned' || status === 'InProgress'),
    canClose: isManager && status === 'Resolved',
    hasManagementActions: (isAssignedStaff && STAFF_ACTIONABLE_STATUSES.has(status))
      || (isManager && MANAGER_ASSIGNABLE_STATUSES.has(status))
      || (isManager && status === 'Resolved'),
  };
};

export const getAssignedListActionLabel = (role, status) => (
  role === 'SupportStaff' && STAFF_ACTIONABLE_STATUSES.has(status) ? 'Xử lý' : 'Chi tiết'
);
