import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Navbar } from './components/Navbar';
import { RegisterView } from './views/RegisterView';
import { LoginView } from './views/LoginView';
import { ProfileView } from './views/ProfileView';
import { PickupBookingView } from './views/PickupBookingView';
import { DashboardView } from './views/DashboardView';
import { ForgotPasswordView } from './views/ForgotPasswordView'; 
import { ResetPasswordView } from './views/ResetPasswordView';
import { ScheduleManagementView } from './views/ScheduleManagementView';

export const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div className="app-container">
          <Navbar />
          <main className="main-content">
            <Routes>
              <Route path="/" element={<Navigate to="/register" replace />} />
              <Route path="/register" element={<RegisterView />} />
              <Route path="/login" element={<LoginView />} />
              <Route path="/dashboard" element={<ProtectedRoute><DashboardView /></ProtectedRoute>} />
              <Route path="/profile" element={<ProtectedRoute><ProfileView /></ProtectedRoute>} />
              <Route path="/pickup" element={<ProtectedRoute><PickupBookingView /></ProtectedRoute>} />
              <Route path="/forgot-password" element={<ForgotPasswordView />} />
              <Route path="/reset-password" element={<ResetPasswordView />} />
              <Route path="/schedule" element={<ProtectedRoute rolesAllowed={['recycler']}><ScheduleManagementView /></ProtectedRoute>} />
              <Route path="*" element={<Navigate to="/register" replace />} />
            </Routes>
          </main>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
