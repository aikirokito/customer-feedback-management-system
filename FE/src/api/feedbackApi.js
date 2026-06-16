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
  submitFeedback: (data) => axiosClient.post('/Feedback', normalizeFeedbackPayload(data)),
  getMyFeedbacks: (params) => axiosClient.get('/Feedback/my', { params }).then(asListResponse),
  getFeedbackById: (id) => axiosClient.get(`/Feedback/${id}`),
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

  getCategories: () => Promise.resolve({ data: FEEDBACK_CATEGORIES }),
};

export default feedbackApi;
