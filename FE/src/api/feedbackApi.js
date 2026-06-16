<<<<<<< HEAD
import axiosClient, { asListResponse } from './axiosClient';

export const FEEDBACK_CATEGORIES = [
  { id: 'Complaint', value: 'Complaint', name: 'Complaint' },
  { id: 'Suggestion', value: 'Suggestion', name: 'Suggestion' },
  { id: 'Service', value: 'Service', name: 'Service' },
  { id: 'Product', value: 'Product', name: 'Product' },
  { id: 'Website', value: 'Website', name: 'Website' },
];

const normalizeFeedbackPayload = (data) => ({
  title: data.title,
  description: data.description,
  category: data.category || data.categoryId || 'Complaint',
});

const normalizeStatusPayload = (data) => ({
  newStatus: data.newStatus || data.status,
  reason: data.reason || null,
});

const feedbackApi = {
  submitFeedback: (data) => axiosClient.post('/feedback', normalizeFeedbackPayload(data)),
  getMyFeedbacks: (params) => axiosClient.get('/feedback/my', { params }).then(asListResponse),
  getFeedbackById: (id) => axiosClient.get(`/feedback/${id}`),
  deleteFeedback: (id) => axiosClient.delete(`/feedback/${id}`),

  getAssignedFeedbacks: (params) => axiosClient.get('/feedback', { params }).then(asListResponse),
  getDepartmentFeedbacks: (params) => axiosClient.get('/feedback', { params }).then(asListResponse),
  getAllFeedbacks: (params) => axiosClient.get('/feedback', { params }).then(asListResponse),

  respondToFeedback: (id, data) => axiosClient.post(`/feedback/${id}/responses`, {
    content: data.content || data.message || data.responseText || '',
    isInternal: Boolean(data.isInternal),
  }),
  getResponses: (id) => axiosClient.get(`/feedback/${id}/responses`).then(asListResponse),

  addComment: (id, data) => axiosClient.post(`/feedback/${id}/comments`, {
    content: data.content || data.message || '',
    parentCommentId: data.parentCommentId || null,
  }),
  getComments: (id) => axiosClient.get(`/feedback/${id}/comments`).then(asListResponse),

  updateFeedbackStatus: (id, data) => axiosClient.patch(`/feedback/${id}/status`, normalizeStatusPayload(data)),
  assignFeedback: (id, data) => axiosClient.patch(`/feedback/${id}/assign`, {
    assignToUserId: data.assignToUserId || data.assignedToUserId || data.staffId,
    note: data.note || data.reason || null,
  }),
  reassignFeedback: (id, data) => axiosClient.patch(`/feedback/${id}/reassign`, {
    assignToUserId: data.assignToUserId || data.assignedToUserId || data.staffId,
    note: data.note || data.reason || null,
  }),
  getAssignments: (id) => axiosClient.get(`/feedback/${id}/assignments`).then(asListResponse),
  managePriority: (id, data) => axiosClient.patch(`/feedback/${id}/priority`, {
    priority: data.priority,
  }),

  uploadAttachment: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return axiosClient.post(`/feedback/${id}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  deleteAttachment: (feedbackId, attachmentId) => axiosClient.delete(`/feedback/${feedbackId}/attachments/${attachmentId}`),

  getCategories: () => Promise.resolve({ data: FEEDBACK_CATEGORIES }),
=======
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
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
};

export default feedbackApi;
