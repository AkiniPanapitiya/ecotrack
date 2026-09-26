import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { useAuth } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Navbar } from './components/Navbar';
import { RegisterView } from './views/RegisterView';
import { LoginView } from './views/LoginView';
import { ProfileView } from './views/ProfileView';
import { KycView } from './views/KycView';
import { KycAdminView } from './views/KycAdminView';
import { AdminUsersView } from './views/AdminUsersView';
import { AccessDeniedView } from './views/AccessDeniedView';
import { PickupBookingView } from './views/PickupBookingView';
import { DashboardView } from './views/DashboardView';
import { ForgotPasswordView } from './views/ForgotPasswordView'; 
import { ResetPasswordView } from './views/ResetPasswordView';
import { ScheduleManagementView } from './views/ScheduleManagementView';
import  MyPickupsView  from './views/MyPickupsView';
import { AuditReportView } from './views/AuditReportView';
import ValuationView from './views/ValuationView';
import { RecyclerPickupBookingView } from './views/RecyclerPickupBookingView';
import MarketplaceView from './views/MarketplaceView';
import CreateListingView from './views/CreateListingView';

const HomeRedirect = () => {
  const { isAuthenticated } = useAuth();
  return <Navigate to={isAuthenticated ? '/dashboard' : '/register'} replace />;
};

export const App = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <div className="app-container">
          <Navbar />
          <main className="main-content">
            <Routes>
              <Route path="/register" element={<RegisterView />} />
              <Route path="/login" element={<LoginView />} />
              <Route path="/dashboard" element={<ProtectedRoute><DashboardView /></ProtectedRoute>} />
              <Route path="/profile" element={<ProtectedRoute><ProfileView /></ProtectedRoute>} />
              <Route path="/pickup" element={<ProtectedRoute><PickupBookingView /></ProtectedRoute>} />
              <Route path="/forgot-password" element={<ForgotPasswordView />} />
              <Route path="/reset-password" element={<ResetPasswordView />} />
              <Route path="/schedule" element={<ProtectedRoute rolesAllowed={['Recycler']}><ScheduleManagementView /></ProtectedRoute>} />
              <Route path="/my-pickups" element={<ProtectedRoute><MyPickupsView /></ProtectedRoute>} />
              <Route path="/valuations" element={<ProtectedRoute rolesAllowed={['Recycler']}><ValuationView /></ProtectedRoute>} />
              <Route path="/audit-report" element={<ProtectedRoute><AuditReportView /></ProtectedRoute>} />
              <Route path="/kyc" element={<ProtectedRoute rolesAllowed={['Recycler']}><KycView /></ProtectedRoute>} />
              <Route path="/kyc-admin" element={<ProtectedRoute rolesAllowed={['Admin']}><KycAdminView /></ProtectedRoute>} />
                  <Route path="/admin/users" element={<ProtectedRoute rolesAllowed={['Admin']}><AdminUsersView /></ProtectedRoute>} />
                  <Route path="/access-denied" element={<AccessDeniedView />} />
                  <Route path="/marketplace" element={<MarketplaceView />} />
                  <Route path="/create-listing" element={<ProtectedRoute rolesAllowed={['Recycler']}><CreateListingView /></ProtectedRoute>} />
                  <Route path="/" element={<HomeRedirect />} />
            </Routes>
          </main>
        </div>
      </AuthProvider>
    </BrowserRouter>
  );
};

export default App;
