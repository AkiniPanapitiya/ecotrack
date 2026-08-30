import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Leaf, User, Shield, LogOut, FileText, LayoutDashboard, Building2, Truck } from 'lucide-react';

export const Navbar = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
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

            <Link to="/pickup" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Truck size={18} />
              <span>Book Pickup</span>
            </Link>

            <Link to="/profile" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <User size={18} />
              <span>Profile</span>
            </Link>

            <Link to="/audit-report" className="nav-link" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <FileText size={18} />
              <span>Audit Report</span>
            </Link>

            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginLeft: '12px', borderLeft: '1px solid var(--border-color)', paddingLeft: '16px' }}>
              <span className={`badge ${user?.role === 'Recycler' ? 'badge-recycler' : 'badge-user'}`}>
                {user?.role === 'Recycler' ? <Building2 size={13} /> : <User size={13} />}
                {user?.role}
              </span>

              <span style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-primary)' }}>
                {user?.fullName}
              </span>

              <button
                onClick={handleLogout}
                className="btn btn-secondary"
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
