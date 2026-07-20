import axiosClient, { asListResponse } from './axiosClient';

const normalizeFeedbackPayload = (data) => ({
  title: data.title,
  description: data.description,
  categoryId: data.categoryId,
});

const normalizeStatusPayload = (data) => ({
  newStatus: data.newStatus || data.status,
  reason: data.reason || null,
});

const feedbackApi = {
  submitFeedback: (data) => axiosClient.post('/Feedback', normalizeFeedbackPayload(data)),
  getMyFeedbacks: (params) => axiosClient.get('/Feedback/my', { params }).then(asListResponse),
  getFeedbackById: (id) => axiosClient.get(`/Feedback/${id}`),
  updateFeedback: (id, data) => axiosClient.put(`/Feedback/${id}`, data),
  deleteFeedback: (id) => axiosClient.delete(`/Feedback/${id}`),

  getAssignedFeedbacks: (params) => axiosClient.get('/Feedback', { params }).then(asListResponse),
  getDepartmentFeedbacks: (params) => axiosClient.get('/Feedback', { params }).then(asListResponse),
  getAllFeedbacks: (params) => axiosClient.get('/Feedback', { params }).then(asListResponse),

  respondToFeedback: (id, data) => axiosClient.post(`/Feedback/${id}/responses`, {
    content: data.content || data.message || data.responseText || '',
    isInternal: Boolean(data.isInternal),
  }),
  getResponses: (id) => axiosClient.get(`/Feedback/${id}/responses`).then(asListResponse),

  addComment: (id, data) => axiosClient.post(`/Feedback/${id}/comments`, {
    content: data.content || data.message || '',
    parentCommentId: data.parentCommentId || null,
  }),
  getComments: (id) => axiosClient.get(`/Feedback/${id}/comments`).then(asListResponse),
  updateComment: (feedbackId, commentId, content) =>
    axiosClient.put(`/Feedback/${feedbackId}/comments/${commentId}`, { content }),
  deleteComment: (feedbackId, commentId) =>
    axiosClient.delete(`/Feedback/${feedbackId}/comments/${commentId}`),
  updateResponse: (feedbackId, responseId, content) =>
    axiosClient.put(`/Feedback/${feedbackId}/responses/${responseId}`, { content }),
  deleteResponse: (feedbackId, responseId) =>
    axiosClient.delete(`/Feedback/${feedbackId}/responses/${responseId}`),

  updateFeedbackStatus: (id, data) => axiosClient.patch(`/Feedback/${id}/status`, normalizeStatusPayload(data)),
  assignFeedback: (id, data) => axiosClient.patch(`/Feedback/${id}/assign`, {
    assignToUserId: data.assignToUserId || data.assignedToUserId || data.staffId,
    note: data.note || data.reason || null,
  }),
  reassignFeedback: (id, data) => axiosClient.patch(`/Feedback/${id}/reassign`, {
    assignToUserId: data.assignToUserId || data.assignedToUserId || data.staffId,
    note: data.note || data.reason || null,
  }),
  managePriority: (id, data) => axiosClient.patch(`/Feedback/${id}/priority`, {
    priority: data.priority,
  }),
  rateFeedback: (id, rating) => axiosClient.patch(`/Feedback/${id}/rating`, { rating: Number(rating) }),
  getAssignmentHistory: (id) => axiosClient.get(`/Feedback/${id}/assignments`).then(asListResponse),
  unassignFeedback: (id) => axiosClient.delete(`/Feedback/${id}/assignments`),

  uploadAttachment: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return axiosClient.post(`/Feedback/${id}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  deleteAttachment: (feedbackId, attachmentId) =>
    axiosClient.delete(`/Feedback/${feedbackId}/attachments/${attachmentId}`),

  getCategories: () => axiosClient.get('/Categories'),
  getAllCategories: () => axiosClient.get('/admin/categories'),
  createCategory: (data) => axiosClient.post('/admin/categories', data),
  updateCategory: (id, data) => axiosClient.patch(`/admin/categories/${id}`, data),
};

export default feedbackApi;
