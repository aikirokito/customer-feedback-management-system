<<<<<<< HEAD
import axiosClient, { asListResponse } from './axiosClient';

const userApi = {
  getAllUsers: (params) => axiosClient.get('/admin/users', { params }).then(asListResponse),
  getUserById: (id) => axiosClient.get(`/users/${id}`),
  updateMyProfile: (data) => axiosClient.put('/users/me', data),
  updateUserRole: (id, role) => axiosClient.patch(`/users/${id}/role`, { role }),
  deactivateUser: (id) => axiosClient.patch(`/users/${id}/deactivate`),
  reactivateUser: (id) => axiosClient.patch(`/users/${id}/reactivate`),
  deleteUser: (id) => axiosClient.delete(`/users/${id}`),

  getReports: (params) => axiosClient.get('/reports/summary', { params }),
  getFeedbackByStatus: (params) => axiosClient.get('/reports/feedback-by-status', { params }),
  getFeedbackByCategory: (params) => axiosClient.get('/reports/feedback-by-category', { params }),
  getFeedbackByPriority: (params) => axiosClient.get('/reports/feedback-by-priority', { params }),
  getStaffWorkload: (params) => axiosClient.get('/reports/staff-workload', { params }).then(asListResponse),

  getAuditLogs: (params) => axiosClient.get('/admin/audit-logs', { params }).then(asListResponse),
=======
import axiosClient from './axiosClient';

const userApi = {
  getAllUsers: (params) => axiosClient.get('/users', { params }),
  getUserById: (id) => axiosClient.get(`/users/${id}`),
  updateUser: (id, data) => axiosClient.put(`/users/${id}`, data),
  deleteUser: (id) => axiosClient.delete(`/users/${id}`),
  getReports: () => axiosClient.get('/reports'),
  getAuditLogs: (params) => axiosClient.get('/audit-logs', { params }),
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
};

export default userApi;
