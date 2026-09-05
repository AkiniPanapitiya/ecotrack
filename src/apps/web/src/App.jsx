import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { RegisterView } from './views/RegisterView';
import { LoginView } from './views/LoginView';
import { ProfileView } from './views/ProfileView';
import { PickupBookingView } from './views/PickupBookingView';
import { DashboardView } from './views/DashboardView';
import { ForgotPasswordView } from './views/ForgotPasswordView'; 
import { ResetPasswordView } from './views/ResetPasswordView';

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
              <Route path="/dashboard" element={<DashboardView />} />
              <Route path="/profile" element={<ProfileView />} />
              <Route path="/pickup" element={<PickupBookingView />} />
              <Route path="/forgot-password" element={<ForgotPasswordView />} />
              <Route path="/reset-password" element={<ResetPasswordView />} />
              <Route path="*" element={<Navigate to="/register" replace />} />
            </Routes>
          </main>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
