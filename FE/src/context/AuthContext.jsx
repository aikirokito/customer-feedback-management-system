import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import authApi from '../api/authApi';

const AuthContext = createContext(null);

export const APP_ROLES = {
  CUSTOMER: 'Customer',
  SUPPORT_STAFF: 'SupportStaff',
  MANAGER: 'DepartmentManager',
  ADMIN: 'SystemAdmin',
};

const ROLE_ALIASES = {
  CUSTOMER: APP_ROLES.CUSTOMER,
  Customer: APP_ROLES.CUSTOMER,
  SUPPORT_STAFF: APP_ROLES.SUPPORT_STAFF,
  SupportStaff: APP_ROLES.SUPPORT_STAFF,
  MANAGER: APP_ROLES.MANAGER,
  DepartmentManager: APP_ROLES.MANAGER,
  ADMIN: APP_ROLES.ADMIN,
  SystemAdmin: APP_ROLES.ADMIN,
};

export const normalizeRole = (roleValue) => ROLE_ALIASES[roleValue] || roleValue;

export const getDefaultRouteForRole = (roleOrUser) => {
  const roleValue = typeof roleOrUser === 'string'
    ? roleOrUser
    : roleOrUser?.roleName || roleOrUser?.role;

  const role = normalizeRole(roleValue);

  switch (role) {
    case APP_ROLES.CUSTOMER:
      return '/dashboard';
    case APP_ROLES.SUPPORT_STAFF:
    case APP_ROLES.MANAGER:
      return '/assigned-feedbacks';
    case APP_ROLES.ADMIN:
      return '/manage-users';
    default:
      return '/dashboard';
  }
};

const normalizeUser = (user) => {
  if (!user) return null;
  const roleValue = user.roleName || user.role;
  const role = normalizeRole(roleValue);

  return {
    ...user,
    role,
    roleName: role,
    fullName: user.fullName || `${user.firstName || ''} ${user.lastName || ''}`.trim(),
  };
};

const clearStoredAuth = () => {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    try {
      const token = localStorage.getItem('accessToken');
      const stored = localStorage.getItem('user');

      if (!token || !stored) return null;
      return normalizeUser(JSON.parse(stored));
    } catch {
      clearStoredAuth();
      return null;
    }
  });
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');

    if (!token) {
      clearStoredAuth();
      setUser(null);
      setInitializing(false);
      return;
    }

    // Always verify the token against the backend on app start.
    // This prevents stale localStorage roles after changing roles in Supabase.
    authApi.getProfile()
      .then((res) => {
        const normalized = normalizeUser(res.data);
        setUser(normalized);
        localStorage.setItem('user', JSON.stringify(normalized));
      })
      .catch(() => {
        clearStoredAuth();
        setUser(null);
      })
      .finally(() => setInitializing(false));
  }, []);

  const login = useCallback(async (credentials) => {
    setLoading(true);
    try {
      const res = await authApi.login({
        email: credentials.email?.trim(),
        password: credentials.password,
      });
      const auth = res.data;

      if (!auth?.accessToken || !auth?.refreshToken || !auth?.user) {
        return {
          success: false,
          message: 'Phản hồi đăng nhập không hợp lệ từ máy chủ.',
        };
      }

      const userData = normalizeUser(auth.user);

      localStorage.setItem('accessToken', auth.accessToken);
      localStorage.setItem('refreshToken', auth.refreshToken);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);

      return { success: true, user: userData, redirectTo: getDefaultRouteForRole(userData) };
    } catch (err) {
      const message = err.normalizedMessage || 'Đăng nhập thất bại. Vui lòng thử lại.';
      return { success: false, message };
    } finally {
      setLoading(false);
    }
  }, []);

  const googleLogin = useCallback(async (idToken) => {
    setLoading(true);
    try {
      const res = await authApi.googleLogin(idToken);
      const auth = res.data;
      if (!auth?.accessToken || !auth?.refreshToken || !auth?.user) {
        return { success: false, message: 'Phản hồi đăng nhập Google không hợp lệ.' };
      }

      const userData = normalizeUser(auth.user);
      localStorage.setItem('accessToken', auth.accessToken);
      localStorage.setItem('refreshToken', auth.refreshToken);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);
      return { success: true, user: userData, redirectTo: getDefaultRouteForRole(userData) };
    } catch (err) {
      return { success: false, message: err.normalizedMessage || 'Đăng nhập Google thất bại.' };
    } finally {
      setLoading(false);
    }
  }, []);

  const register = useCallback(async (data) => {
    setLoading(true);
    try {
      const res = await authApi.register(data);
      return { success: true, data: res.data };
    } catch (err) {
      const message = err.normalizedMessage || 'Đăng ký thất bại. Vui lòng thử lại.';
      return { success: false, message };
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = localStorage.getItem('refreshToken');
    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } catch {
      // Ignore server-side logout errors so the local session is still cleared.
    } finally {
      clearStoredAuth();
      setUser(null);
    }
  }, []);

  const updateCurrentUser = useCallback((changes) => {
    setUser((current) => {
      const normalized = normalizeUser({ ...current, ...changes });
      localStorage.setItem('user', JSON.stringify(normalized));
      return normalized;
    });
  }, []);

  useEffect(() => {
    const handleSessionRefresh = (event) => updateCurrentUser(event.detail || {});
    window.addEventListener('cfms:session-refreshed', handleSessionRefresh);
    return () => window.removeEventListener('cfms:session-refreshed', handleSessionRefresh);
  }, [updateCurrentUser]);

  const hasRole = useCallback((roles) => {
    if (!user) return false;
    const allowed = Array.isArray(roles) ? roles : [roles];
    return allowed.map(role => normalizeRole(role)).includes(user.role);
  }, [user]);

  return (
    <AuthContext.Provider value={{ user, loading, initializing, login, googleLogin, register, logout, hasRole, updateCurrentUser }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};

export default AuthContext;
