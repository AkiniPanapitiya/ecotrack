import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Leaf, User, Shield, LogOut, FileText, LayoutDashboard, Building2, Truck, PackageSearch } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const Navbar = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login', {state: {message: 'You have been logged out successfully.'}});
  };

  return (
    <header className="navbar">
      <Link to="/" className="nav-brand">
        <Leaf size={24} style={{ color: '#10b981' }} />
        <span>EcoTrack</span>
      </Link>

      <nav className="nav-links">
        {isAuthenticated ? (
          <>
            <Link to="/dashboard" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <LayoutDashboard size={18} />
              <span>Dashboard</span>
            </Link>

          {user?.role === 'User' && (
            <Link to="/pickup" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Truck size={18} />
              <span>Book Pickup</span>
            </Link>
          )}

          {user?.role === 'User' && (
          <Link to="/my-pickups" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <PackageSearch size={18} />
              <span>My Pickups</span>
          </Link>
          )}
            
            {user?.role === 'Recycler' && (
              <>
                <Link to="/schedule" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Shield size={18} />
                  <span>Schedule Management</span>
                </Link>
                <Link to="/kyc" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Shield size={18} />
                  <span>KYC Verification</span>
                </Link>
              </>
            )}

            {user?.role === 'Admin' && (
              <>
                <Link to="/audit-report" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <FileText size={18} />
                  <span>Audit Report</span>
                </Link>
                <Link to="/kyc-admin" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Shield size={18} />
                  <span>KYC Admin</span>
                </Link>
              </>
            )}

            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginLeft: '12px', borderLeft: '1px solid var(--border-color)', paddingLeft: '16px' }}>
              <span className={`badge ${user?.role === 'Recycler' ? 'badge-recycler' : 'badge-user'}`}>
                {user?.role === 'Recycler' ? <Building2 size={13} /> : <User size={13} />}
                {user?.role}
              </span>

              <Link
                to="/profile"
                style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-primary)' }}
              >
                <User size={16} />
                <span>Profile</span>
              </Link>

              <button
                onClick={handleLogout}
                className="btn btn-logout"
                style={{ padding: '0.45rem 0.85rem', fontSize: '0.85rem' }}
                title="Log out"
              >
                <LogOut size={16} />
                <span>Logout</span>
              </button>
            </div>
          </>
        ) : (
          <>
            <Link to="/login" className="nav-link">Login</Link>
            <Link to="/register" className="btn btn-primary" style={{ padding: '0.5rem 1.25rem' }}>
              Get Started
            </Link>
          </>
        )}
      </nav>
    </header>
  );
};
