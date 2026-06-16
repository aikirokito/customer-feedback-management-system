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

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem('user');
      return stored ? normalizeUser(JSON.parse(stored)) : null;
    } catch {
      return null;
    }
  });
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (token && !user) {
      authApi.getProfile()
        .then((res) => {
          const normalized = normalizeUser(res.data);
          setUser(normalized);
          localStorage.setItem('user', JSON.stringify(normalized));
        })
        .catch(() => {
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
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
      const auth = res.data;
      const userData = normalizeUser(auth.user);

      localStorage.setItem('accessToken', auth.accessToken);
      localStorage.setItem('refreshToken', auth.refreshToken);
      localStorage.setItem('user', JSON.stringify(userData));
      setUser(userData);

      return { success: true, user: userData };
    } catch (err) {
      const message = err.normalizedMessage || 'Đăng nhập thất bại. Vui lòng thử lại.';
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
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      setUser(null);
    }
  }, []);

  const hasRole = useCallback((roles) => {
    if (!user) return false;
    const allowed = Array.isArray(roles) ? roles : [roles];
    return allowed.map(role => ROLE_ALIASES[role] || role).includes(user.role);
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
