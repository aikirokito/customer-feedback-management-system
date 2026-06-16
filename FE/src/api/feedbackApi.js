import axiosClient from './axiosClient';

const feedbackApi = {
  // Customer
  submitFeedback: (data) => axiosClient.post('/feedbacks', data),
  getMyFeedbacks: () => axiosClient.get('/feedbacks/my'),
  getFeedbackById: (id) => axiosClient.get(`/feedbacks/${id}`),

  // Support Staff / Manager
  getAssignedFeedbacks: () => axiosClient.get('/feedbacks/assigned'),
  getDepartmentFeedbacks: () => axiosClient.get('/feedbacks/department'),
  respondToFeedback: (id, data) => axiosClient.post(`/feedbacks/${id}/respond`, data),
  updateFeedbackStatus: (id, data) => axiosClient.patch(`/feedbacks/${id}/status`, data),

  // Manager / Admin
  getAllFeedbacks: (params) => axiosClient.get('/feedbacks', { params }),
  assignFeedback: (id, data) => axiosClient.patch(`/feedbacks/${id}/assign`, data),
  managePriority: (id, data) => axiosClient.patch(`/feedbacks/${id}/priority`, data),

  // Categories
  getCategories: () => axiosClient.get('/categories'),
};

export default feedbackApi;
