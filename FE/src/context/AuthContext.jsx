import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import authApi from '../api/authApi';

const AuthContext = createContext(null);

<<<<<<< HEAD
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

const normalizeUser = (user) => {
  if (!user) return null;
  const roleValue = user.roleName || user.role;
  const role = ROLE_ALIASES[roleValue] || roleValue;

  return {
    ...user,
    role,
    roleName: role,
    fullName: user.fullName || `${user.firstName || ''} ${user.lastName || ''}`.trim(),
  };
};

=======
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem('user');
<<<<<<< HEAD
      return stored ? normalizeUser(JSON.parse(stored)) : null;
=======
      return stored ? JSON.parse(stored) : null;
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
    } catch {
      return null;
    }
  });
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);

<<<<<<< HEAD
=======
  // On mount: verify token is still valid
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (token && !user) {
      authApi.getProfile()
        .then((res) => {
<<<<<<< HEAD
          const normalized = normalizeUser(res.data);
          setUser(normalized);
          localStorage.setItem('user', JSON.stringify(normalized));
        })
        .catch(() => {
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
=======
          setUser(res.data);
          localStorage.setItem('user', JSON.stringify(res.data));
        })
        .catch(() => {
          localStorage.removeItem('accessToken');
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
          localStorage.removeItem('user');
          setUser(null);
        })
        .finally(() => setInitializing(false));
    } else {
      setInitializing(false);
    }
  }, []);

  const login = useCallback(async (credentials) => {
    setLoading(true);
    try {
      const res = await authApi.login(credentials);
<<<<<<< HEAD
      const auth = res.data;
      const userData = normalizeUser(auth.user);

      localStorage.setItem('accessToken', auth.accessToken);
      localStorage.setItem('refreshToken', auth.refreshToken);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);

      return { success: true, user: userData };
    } catch (err) {
      const message = err.normalizedMessage || 'Đăng nhập thất bại. Vui lòng thử lại.';
=======
      const { token, user: userData } = res.data;
      localStorage.setItem('accessToken', token);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);
      return { success: true, user: userData };
    } catch (err) {
      const message = err.response?.data?.message || 'Đăng nhập thất bại. Vui lòng thử lại.';
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
      return { success: false, message };
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
<<<<<<< HEAD
      const message = err.normalizedMessage || 'Đăng ký thất bại. Vui lòng thử lại.';
=======
      const message = err.response?.data?.message || 'Đăng ký thất bại. Vui lòng thử lại.';
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
      return { success: false, message };
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
<<<<<<< HEAD
    const refreshToken = localStorage.getItem('refreshToken');
    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } catch {
      // Ignore server-side logout errors so the local session is still cleared.
    } finally {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
=======
    try {
      await authApi.logout();
    } catch {
      // ignore server-side error
    } finally {
      localStorage.removeItem('accessToken');
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
      localStorage.removeItem('user');
      setUser(null);
    }
  }, []);

  const hasRole = useCallback((roles) => {
    if (!user) return false;
<<<<<<< HEAD
    const allowed = Array.isArray(roles) ? roles : [roles];
    return allowed.map(role => ROLE_ALIASES[role] || role).includes(user.role);
=======
    if (Array.isArray(roles)) return roles.includes(user.role);
    return user.role === roles;
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
  }, [user]);

  return (
    <AuthContext.Provider value={{ user, loading, initializing, login, register, logout, hasRole }}>
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
