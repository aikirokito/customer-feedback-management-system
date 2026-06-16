import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './ProtectedRoute';
import RoleBasedRoute from './RoleBasedRoute';

import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import DashboardPage from '../pages/DashboardPage';
import SubmitFeedbackPage from '../pages/SubmitFeedbackPage';
import MyFeedbacksPage from '../pages/MyFeedbacksPage';
import FeedbackDetailPage from '../pages/FeedbackDetailPage';
import AssignedFeedbacksPage from '../pages/AssignedFeedbacksPage';
import ManageUsersPage from '../pages/ManageUsersPage';
import ReportsPage from '../pages/ReportsPage';
import AuditLogsPage from '../pages/AuditLogsPage';
import UnauthorizedPage from '../pages/UnauthorizedPage';
import NotFoundPage from '../pages/NotFoundPage';
import MainLayout from '../layouts/MainLayout';

const AppRoutes = () => {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* Protected – all authenticated users */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />

        {/* Customer */}
        <Route path="submit-feedback" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['Customer']}>
=======
          <RoleBasedRoute allowedRoles={['CUSTOMER', 'ADMIN']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <SubmitFeedbackPage />
          </RoleBasedRoute>
        } />
        <Route path="my-feedbacks" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['Customer']}>
=======
          <RoleBasedRoute allowedRoles={['CUSTOMER', 'ADMIN']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <MyFeedbacksPage />
          </RoleBasedRoute>
        } />

        {/* Support Staff + Manager + Admin */}
        <Route path="assigned-feedbacks" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['SupportStaff', 'DepartmentManager', 'SystemAdmin']}>
=======
          <RoleBasedRoute allowedRoles={['SUPPORT_STAFF', 'MANAGER', 'ADMIN']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <AssignedFeedbacksPage />
          </RoleBasedRoute>
        } />

        {/* Feedback detail – multiple roles */}
        <Route path="feedbacks/:id" element={
          <ProtectedRoute>
            <FeedbackDetailPage />
          </ProtectedRoute>
        } />

        {/* Admin only */}
        <Route path="manage-users" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
=======
          <RoleBasedRoute allowedRoles={['ADMIN']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <ManageUsersPage />
          </RoleBasedRoute>
        } />
        <Route path="reports" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['SystemAdmin', 'DepartmentManager']}>
=======
          <RoleBasedRoute allowedRoles={['ADMIN', 'MANAGER']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <ReportsPage />
          </RoleBasedRoute>
        } />
        <Route path="audit-logs" element={
<<<<<<< HEAD
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
=======
          <RoleBasedRoute allowedRoles={['ADMIN']}>
>>>>>>> b1f8e2620e3cb306a06b977c0e072848a468c397
            <AuditLogsPage />
          </RoleBasedRoute>
        } />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
};

export default AppRoutes;
