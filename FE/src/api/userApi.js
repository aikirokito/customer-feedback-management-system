import axiosClient from './axiosClient';

const userApi = {
  getAllUsers: (params) => axiosClient.get('/users', { params }),
  getUserById: (id) => axiosClient.get(`/users/${id}`),
  updateUser: (id, data) => axiosClient.put(`/users/${id}`, data),
  deleteUser: (id) => axiosClient.delete(`/users/${id}`),
  getReports: () => axiosClient.get('/reports'),
  getAuditLogs: (params) => axiosClient.get('/audit-logs', { params }),
};

export default userApi;
