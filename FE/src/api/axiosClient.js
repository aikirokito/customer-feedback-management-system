import axios from 'axios';

<<<<<<< HEAD
const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

const unwrapApiResponse = (payload) => {
  if (
    payload &&
    typeof payload === 'object' &&
    Object.prototype.hasOwnProperty.call(payload, 'success') &&
    Object.prototype.hasOwnProperty.call(payload, 'data')
  ) {
    return payload.data;
  }

  return payload;
};

const getErrorMessage = (error) => {
  const payload = error.response?.data;
  if (!payload) return error.message || 'Có lỗi xảy ra.';

  if (typeof payload === 'string') return payload;
  if (Array.isArray(payload.errors)) return payload.errors.join('\n');
  if (typeof payload.errors === 'object' && payload.errors !== null) {
    return Object.values(payload.errors).flat().join('\n');
  }

  return payload.message || payload.title || error.message || 'Có lỗi xảy ra.';
};
=======
const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api';
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397

const axiosClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

<<<<<<< HEAD
=======
// Request interceptor – attach JWT token
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
<<<<<<< HEAD
      config.headers.Authorization = `Bearer ${token}`;
=======
      config.headers['Authorization'] = `Bearer ${token}`;
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    }
    return config;
  },
  (error) => Promise.reject(error)
);

<<<<<<< HEAD
axiosClient.interceptors.response.use(
  (response) => {
    response.data = unwrapApiResponse(response.data);
    return response;
  },
  (error) => {
    error.normalizedMessage = getErrorMessage(error);

    if (error.response?.status === 401) {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login';
      }
    }

=======
// Response interceptor – handle 401 globally
axiosClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    return Promise.reject(error);
  }
);

<<<<<<< HEAD
export const asListResponse = (response) => {
  const data = response.data;
  if (Array.isArray(data)) return response;
  if (Array.isArray(data?.items)) {
    return {
      ...response,
      data: data.items,
      pagination: {
        page: data.page,
        pageSize: data.pageSize,
        totalCount: data.totalCount,
        totalPages: data.totalPages,
        hasNextPage: data.hasNextPage,
        hasPreviousPage: data.hasPreviousPage,
      },
    };
  }
  return response;
};

=======
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
export default axiosClient;
