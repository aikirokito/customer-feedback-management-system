import axiosClient from './axiosClient';

const normalizeRefreshTokenPayload = (value) => {
  if (typeof value === 'string') return { refreshToken: value };
  return value || { refreshToken: localStorage.getItem('refreshToken') || '' };
};

const authApi = {
  login: (data) => axiosClient.post('/auth/login', data),
  register: (data) => axiosClient.post('/auth/register', data),
  logout: (refreshToken) => axiosClient.post('/auth/logout', normalizeRefreshTokenPayload(refreshToken)),
  getProfile: () => axiosClient.get('/users/me'),
  refreshToken: (refreshToken) => axiosClient.post('/auth/refresh-token', normalizeRefreshTokenPayload(refreshToken)),
  googleLogin: (idToken) => axiosClient.post('/auth/google-login', { idToken }),
  changePassword: (data) => axiosClient.post('/auth/change-password', data),
};

export default authApi;
