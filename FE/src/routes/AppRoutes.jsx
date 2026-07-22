import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from './ProtectedRoute';
import RoleBasedRoute from './RoleBasedRoute';
import { getDefaultRouteForRole, useAuth } from '../context/AuthContext';

import LoginPage from '../pages/LoginPage';
import RegisterPage from '../pages/RegisterPage';
import DashboardPage from '../pages/DashboardPage';
import SubmitFeedbackPage from '../pages/SubmitFeedbackPage';
import MyFeedbacksPage from '../pages/MyFeedbacksPage';
import FeedbackDetailPage from '../pages/FeedbackDetailPage';
import AssignedFeedbacksPage from '../pages/AssignedFeedbacksPage';
import ManageUsersPage from '../pages/ManageUsersPage';
import ReportsPage from '../pages/ReportsPage';
import ManageCategoriesPage from '../pages/ManageCategoriesPage';
import NotificationsPage from '../pages/NotificationsPage';
import ProfilePage from '../pages/ProfilePage';
import UnauthorizedPage from '../pages/UnauthorizedPage';
import NotFoundPage from '../pages/NotFoundPage';
import MainLayout from '../layouts/MainLayout';

const RoleHomeRedirect = () => {
  const { user } = useAuth();
  return <Navigate to={getDefaultRouteForRole(user)} replace />;
};

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
        <Route index element={<RoleHomeRedirect />} />
        <Route path="dashboard" element={
          <RoleBasedRoute allowedRoles={['Customer', 'SupportStaff', 'DepartmentManager']}>
            <DashboardPage />
          </RoleBasedRoute>
        } />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="notifications" element={
          <RoleBasedRoute allowedRoles={['Customer', 'SupportStaff', 'DepartmentManager']}>
            <NotificationsPage />
          </RoleBasedRoute>
        } />

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

        {/* Support Staff + Manager */}
        <Route path="assigned-feedbacks" element={
          <RoleBasedRoute allowedRoles={['SupportStaff', 'DepartmentManager']}>
            <AssignedFeedbacksPage />
          </RoleBasedRoute>
        } />

        {/* Operational feedback detail */}
        <Route path="feedbacks/:id" element={
          <RoleBasedRoute allowedRoles={['Customer', 'SupportStaff', 'DepartmentManager']}>
            <FeedbackDetailPage />
          </RoleBasedRoute>
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
        <Route path="reports" element={
          <RoleBasedRoute allowedRoles={['DepartmentManager']}>
            <ReportsPage />
          </RoleBasedRoute>
        } />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
};

export default AppRoutes;
