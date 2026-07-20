import axios from 'axios';

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

const axiosClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 60000,
});

let refreshPromise = null;

const clearStoredAuth = () => {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
};

const redirectToLogin = () => {
  clearStoredAuth();
  if (!window.location.pathname.includes('/login')) {
    window.location.href = '/login';
  }
};

const refreshSession = async () => {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) throw new Error('No refresh token is available.');

  const response = await axios.post(`${BASE_URL}/Auth/refresh-token`, { refreshToken }, {
    headers: { 'Content-Type': 'application/json' },
    timeout: 60000,
  });
  const auth = unwrapApiResponse(response.data);
  if (!auth?.accessToken || !auth?.refreshToken) {
    throw new Error('The token refresh response is invalid.');
  }

  localStorage.setItem('accessToken', auth.accessToken);
  localStorage.setItem('refreshToken', auth.refreshToken);
  if (auth.user) {
    localStorage.setItem('user', JSON.stringify(auth.user));
    window.dispatchEvent(new CustomEvent('cfms:session-refreshed', { detail: auth.user }));
  }
  return auth.accessToken;
};

axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

axiosClient.interceptors.response.use(
  (response) => {
    response.data = unwrapApiResponse(response.data);
    return response;
  },
  async (error) => {
    error.normalizedMessage = getErrorMessage(error);

    const originalRequest = error.config;
    const isAuthRequest = originalRequest?.url?.includes('/Auth/login') ||
      originalRequest?.url?.includes('/Auth/register') ||
      originalRequest?.url?.includes('/Auth/refresh-token');

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthRequest) {
      originalRequest._retry = true;
      try {
        refreshPromise ||= refreshSession().finally(() => { refreshPromise = null; });
        const accessToken = await refreshPromise;
        originalRequest.headers = originalRequest.headers || {};
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return axiosClient(originalRequest);
      } catch {
        redirectToLogin();
      }
    } else if (error.response?.status === 401 && !isAuthRequest) {
      redirectToLogin();
    }

    return Promise.reject(error);
  }
);

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

export default axiosClient;
