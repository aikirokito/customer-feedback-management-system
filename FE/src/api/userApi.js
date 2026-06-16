import axiosClient, { asListResponse } from './axiosClient';

const userApi = {
  getAllUsers: (params) => axiosClient.get('/Admin/users', { params }).then(asListResponse),
  getUserById: (id) => axiosClient.get(`/Users/${id}`),
  updateMyProfile: (data) => axiosClient.put('/Users/me', data),
  updateUserRole: (id, role) => axiosClient.patch(`/Admin/users/${id}/role`, { role }),
  updateUserStatus: (id, isActive) => axiosClient.patch(`/Admin/users/${id}/status`, { isActive }),
  deleteUser: (id) => axiosClient.delete(`/Users/${id}`),

  getReports: (params) => axiosClient.get('/Reports/summary', { params }),
  getFeedbackByStatus: (params) => axiosClient.get('/Reports/feedback-by-status', { params }),
  getFeedbackByCategory: (params) => axiosClient.get('/Reports/feedback-by-category', { params }),
  getFeedbackByPriority: (params) => axiosClient.get('/Reports/feedback-by-priority', { params }),
  getFeedbackByMonth: (params) => axiosClient.get('/Reports/feedback-by-month', { params }),
  getStaffWorkload: (params) => axiosClient.get('/Reports/staff-workload', { params }).then(asListResponse),

  getAuditLogs: (params) => axiosClient.get('/Admin/audit-logs', { params }).then(asListResponse),

  getNotifications: (params) => axiosClient.get('/Notifications', { params }),
  getUnreadCount: () => axiosClient.get('/Notifications/unread-count'),
  markNotificationRead: (id) => axiosClient.patch(`/Notifications/${id}/read`),
  markAllRead: () => axiosClient.patch('/Notifications/read-all'),
};

export default userApi;
