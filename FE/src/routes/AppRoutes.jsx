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
import ManageCategoriesPage from '../pages/ManageCategoriesPage';
import ManageDepartmentsPage from '../pages/ManageDepartmentsPage';
import NotificationsPage from '../pages/NotificationsPage';
import ProfilePage from '../pages/ProfilePage';
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
        <Route path="profile" element={<ProfilePage />} />
        <Route path="notifications" element={<NotificationsPage />} />

        {/* Customer */}
        <Route path="submit-feedback" element={
          <RoleBasedRoute allowedRoles={['Customer']}>
            <SubmitFeedbackPage />
          </RoleBasedRoute>
        } />
        <Route path="my-feedbacks" element={
          <RoleBasedRoute allowedRoles={['Customer']}>
            <MyFeedbacksPage />
          </RoleBasedRoute>
        } />

        {/* Support Staff + Manager + Admin */}
        <Route path="assigned-feedbacks" element={
          <RoleBasedRoute allowedRoles={['SupportStaff', 'DepartmentManager', 'SystemAdmin']}>
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
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
            <ManageUsersPage />
          </RoleBasedRoute>
        } />
        <Route path="manage-categories" element={
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
            <ManageCategoriesPage />
          </RoleBasedRoute>
        } />
        <Route path="manage-departments" element={
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
            <ManageDepartmentsPage />
          </RoleBasedRoute>
        } />
        <Route path="reports" element={
          <RoleBasedRoute allowedRoles={['SystemAdmin', 'DepartmentManager']}>
            <ReportsPage />
          </RoleBasedRoute>
        } />
        <Route path="audit-logs" element={
          <RoleBasedRoute allowedRoles={['SystemAdmin']}>
            <AuditLogsPage />
          </RoleBasedRoute>
        } />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
};

export default AppRoutes;
