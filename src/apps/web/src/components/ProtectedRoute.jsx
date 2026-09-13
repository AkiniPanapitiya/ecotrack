import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const ProtectedRoute = ({ children, rolesAllowed }) => {
  const { isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // Not logged in bounce to login
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (rolesAllowed && !rolesAllowed.includes(user?.role)) {
    // Logged in, but wrong role bounce to dashboard
    return <Navigate to="/dashboard" replace />;
  }

  return children;
};