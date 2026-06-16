import axiosClient from './axiosClient';

<<<<<<< HEAD
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
=======
const authApi = {
  login: (data) => axiosClient.post('/auth/login', data),
  register: (data) => axiosClient.post('/auth/register', data),
  logout: () => axiosClient.post('/auth/logout'),
  getProfile: () => axiosClient.get('/auth/me'),
  refreshToken: (data) => axiosClient.post('/auth/refresh', data),
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
};

export default authApi;
