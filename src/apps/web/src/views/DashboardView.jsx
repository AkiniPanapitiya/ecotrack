import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Leaf, User, ShieldCheck, FileText, ArrowRight, Building2, Clock, CheckCircle2, Truck, PackageSearch, Shield } from 'lucide-react';

export const DashboardView = () => {
  const { user } = useAuth();

  return (
    <div style={{ maxWidth: '1000px', margin: '1rem auto' }}>
      {/* Welcome Banner */}
      <div className="glass-card" style={{ marginBottom: '2rem', background: 'linear-gradient(135deg, rgba(16, 185, 129, 0.12) 0%, rgba(6, 182, 212, 0.08) 100%)', borderColor: 'rgba(16, 185, 129, 0.25)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1.5rem' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '0.5rem' }}>
              <span className={`badge ${user?.role === 'Recycler' ? 'badge-recycler' : 'badge-user'}`}>
                {user?.role === 'Recycler' ? <Building2 size={13} /> : <User size={13} />}
                {user?.role} Portal
              </span>
              {user?.verificationStatus && (
                <span className={`badge ${user.verificationStatus === 'Approved' ? 'badge-approved' : 'badge-pending'}`}>
                  <Clock size={12} />
                  Status: {user.verificationStatus}
                </span>
              )}
            </div>
            <h1 style={{ fontSize: '2rem', fontWeight: 800 }}>Welcome back, {user?.fullName}!</h1>
            <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', marginTop: '0.25rem' }}>
              Logged in as <strong style={{ color: 'var(--text-primary)' }}>{user?.email}</strong>
            </p>
          </div>

          <div style={{ display: 'flex', gap: '10px' }}>
            <Link to="/profile" className="btn btn-primary">
              <User size={18} />
              <span>Manage Profile</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Feature Navigation Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1.5rem', marginBottom: '2.5rem' }}>

        {user?.role === 'User' && (
          <div className="glass-card">
            <div style={{ display: 'inline-flex', padding: '10px', borderRadius: '12px', background: 'rgba(16, 185, 129, 0.15)', color: 'var(--primary)', marginBottom: '1rem' }}>
              <Truck size={24} />
            </div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem' }}>
              Book Pickup
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
              Schedule a new e-waste pickup with item details, weight, and your preferred time slot.
            </p>
            <Link to="/pickup" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: 'var(--primary)', fontSize: '0.9rem', fontWeight: 600 }}>
              <span>Book a Pickup</span>
              <ArrowRight size={16} />
            </Link>
          </div>
        )}

        {user?.role === 'User' && (
          <div className="glass-card">
            <div style={{ display: 'inline-flex', padding: '10px', borderRadius: '12px', background: 'rgba(6, 182, 212, 0.15)', color: 'var(--accent)', marginBottom: '1rem' }}>
              <PackageSearch size={24} />
            </div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem' }}>
              My Pickups
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
              Track the status of your requested pickups, from pending to scheduled and completed.
            </p>
            <Link to="/my-pickups" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: 'var(--accent)', fontSize: '0.9rem', fontWeight: 600 }}>
              <span>View My Pickups</span>
              <ArrowRight size={16} />
            </Link>
          </div>
        )}

        {user?.role === 'Recycler' && (
          <div className="glass-card">
            <div style={{ display: 'inline-flex', padding: '10px', borderRadius: '12px', background: 'rgba(16, 185, 129, 0.15)', color: 'var(--primary)', marginBottom: '1rem' }}>
              <Shield size={24} />
            </div>
            <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem' }}>
              Schedule Management
            </h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
              Review pending pickup requests and confirm a date and time slot with the customer.
            </p>
            <Link to="/schedule" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: 'var(--primary)', fontSize: '0.9rem', fontWeight: 600 }}>
              <span>Manage Schedule</span>
              <ArrowRight size={16} />
            </Link>
          </div>
        )}

        <div className="glass-card">
          <div style={{ display: 'inline-flex', padding: '10px', borderRadius: '12px', background: 'rgba(6, 182, 212, 0.15)', color: 'var(--accent)', marginBottom: '1rem' }}>
            <User size={24} />
          </div>
          <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem' }}>
            Profile Management
          </h3>
          <Link to="/profile" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: 'var(--accent)', fontSize: '0.9rem', fontWeight: 600 }}>
            <span>Edit Profile</span>
            <ArrowRight size={16} />
          </Link>
        </div>

        <div className="glass-card">
          <div style={{ display: 'inline-flex', padding: '10px', borderRadius: '12px', background: 'rgba(245, 158, 11, 0.15)', color: 'var(--warning)', marginBottom: '1rem' }}>
            <FileText size={24} />
          </div>
          <h3 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem' }}>
            Audit & Compliance Log
          </h3>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
            View registration telemetry and authentication audit log reports in real-time.
          </p>
          <Link to="/audit-report" style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', color: 'var(--warning)', fontSize: '0.9rem', fontWeight: 600 }}>
            <span>Open Audit Report</span>
            <ArrowRight size={16} />
          </Link>
        </div>
      </div>
    </div>
  );
};
