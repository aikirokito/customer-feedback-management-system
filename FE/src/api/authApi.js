import axiosClient from './axiosClient';

const normalizeRefreshTokenPayload = (value) => {
  if (typeof value === 'string') return { refreshToken: value };
  return value || { refreshToken: localStorage.getItem('refreshToken') || '' };
};

const authApi = {
  login: (data) => axiosClient.post('/Auth/login', data),
  register: (data) => axiosClient.post('/Auth/register', data),
  logout: (refreshToken) => axiosClient.post('/Auth/logout', normalizeRefreshTokenPayload(refreshToken)),
  getProfile: () => axiosClient.get('/Users/me'),
  refreshToken: (refreshToken) => axiosClient.post('/Auth/refresh-token', normalizeRefreshTokenPayload(refreshToken)),
};

export default authApi;
