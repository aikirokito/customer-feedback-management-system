import axiosClient, { asListResponse } from './axiosClient';

const userApi = {
  getAllUsers: (params) => axiosClient.get('/admin/users', { params }).then(asListResponse),
  getUserById: (id) => axiosClient.get(`/Users/${id}`),
  updateMyProfile: (data) => axiosClient.put('/Users/me', data),

  // Admin user management
  updateUserRole: (id, role) => axiosClient.patch(`/admin/users/${id}/role`, { role }),
  updateUserStatus: (id, isActive) => axiosClient.patch(`/admin/users/${id}/status`, { isActive }),
  deactivateUser: (id) => axiosClient.patch(`/Users/${id}/deactivate`),
  reactivateUser: (id) => axiosClient.patch(`/Users/${id}/reactivate`),
  deleteUser: (id) => axiosClient.delete(`/Users/${id}`),

  // Support Staff lookup (for Manager/Admin assignment UI)
  getSupportStaff: () => axiosClient.get('/Users/support-staff').then(asListResponse),

  // Reports
  getReports: (params) => axiosClient.get('/Reports/summary', { params }),
  getFeedbackByStatus: (params) => axiosClient.get('/Reports/feedback-by-status', { params }),
  getFeedbackByCategory: (params) => axiosClient.get('/Reports/feedback-by-category', { params }),
  getFeedbackByPriority: (params) => axiosClient.get('/Reports/feedback-by-priority', { params }),
  getFeedbackByMonth: (params) => axiosClient.get('/Reports/feedback-by-month', { params }),
  getStaffWorkload: (params) => axiosClient.get('/Reports/staff-workload', { params }).then(asListResponse),

  // Audit Logs
  getAuditLogs: (params) => axiosClient.get('/admin/audit-logs', { params }).then(asListResponse),

  // Notifications
  getNotifications: (params) => axiosClient.get('/Notifications', { params }),
  getUnreadCount: () => axiosClient.get('/Notifications/unread-count'),
  markNotificationRead: (id) => axiosClient.patch(`/Notifications/${id}/read`),
  markAllRead: () => axiosClient.patch('/Notifications/read-all'),
};

export default userApi;
