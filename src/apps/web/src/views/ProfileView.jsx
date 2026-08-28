import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../services/api';
import { User, Mail, Phone, MapPin, Building2, Shield, CheckCircle2, AlertCircle, Save, Clock } from 'lucide-react';

export const ProfileView = () => {
  const { user, refreshProfile } = useAuth();

  const [profile, setProfile] = useState(null);
  const [formData, setFormData] = useState({
    fullName: '',
    phoneNumber: '',
    address: '',
    companyName: '',
    facilityAddress: '',
    operationalCapacityKg: ''
  });

  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    setLoading(true);
    try {
      const res = await authApi.getProfile();
      setProfile(res.data);
      setFormData({
        fullName: res.data.fullName || '',
        phoneNumber: res.data.phoneNumber || '',
        address: res.data.address || '',
        companyName: res.data.recyclerProfile?.companyName || '',
        facilityAddress: res.data.recyclerProfile?.facilityAddress || '',
        operationalCapacityKg: res.data.recyclerProfile?.operationalCapacityKg || ''
      });
    } catch (err) {
      setServerError('Failed to fetch profile information.');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
    setSuccessMessage('');
    setServerError('');
  };

  const validate = () => {
    const newErrors = {};
    if (!formData.fullName.trim()) {
      newErrors.fullName = 'Name is required.';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setSaving(true);
    setSuccessMessage('');
    setServerError('');

    try {
      const payload = {
        fullName: formData.fullName.trim(),
        phoneNumber: formData.phoneNumber.trim() || null,
        address: formData.address.trim() || null,
        companyName: formData.companyName.trim() || null,
        facilityAddress: formData.facilityAddress.trim() || null,
        operationalCapacityKg: formData.operationalCapacityKg ? parseFloat(formData.operationalCapacityKg) : null
      };

      const res = await authApi.updateProfile(payload);
      setProfile(res.data.profile);
      setSuccessMessage(res.data.message || 'Profile updated successfully.');
      await refreshProfile();
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to update profile.';
      setServerError(msg);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <p style={{ color: 'var(--text-secondary)' }}>Loading profile data...</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '800px', margin: '1rem auto' }}>
      <div className="glass-card">
        {/* Profile Header */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '2rem', paddingBottom: '1.5rem', borderBottom: '1px solid var(--border-color)', flexWrap: 'wrap', gap: '1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
            <div style={{ width: '64px', height: '64px', borderRadius: '50%', background: 'linear-gradient(135deg, #10b981 0%, #06b6d4 100%)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.5rem', fontWeight: 800, color: '#fff' }}>
              {profile?.fullName ? profile.fullName.charAt(0).toUpperCase() : 'U'}
            </div>
            <div>
              <h1 style={{ fontSize: '1.5rem', fontWeight: 800 }}>{profile?.fullName}</h1>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>{profile?.email}</p>
            </div>
          </div>

          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <span className={`badge ${profile?.role === 'Recycler' ? 'badge-recycler' : 'badge-user'}`}>
              {profile?.role}
            </span>
            {profile?.role === 'Recycler' && (
              <span className={`badge ${profile?.recyclerProfile?.verificationStatus === 'Approved' ? 'badge-approved' : 'badge-pending'}`}>
                <Clock size={12} />
                {profile?.recyclerProfile?.verificationStatus || 'Pending'}
              </span>
            )}
          </div>
        </div>

        {successMessage && (
          <div className="alert alert-success">
            <CheckCircle2 size={18} />
            <span>{successMessage}</span>
          </div>
        )}

        {serverError && (
          <div className="alert alert-danger">
            <AlertCircle size={18} />
            <span>{serverError}</span>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <h2 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '1.25rem', color: 'var(--text-primary)' }}>
            Personal & Contact Information
          </h2>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Full Name *</label>
              <input
                type="text"
                name="fullName"
                className="form-input"
                value={formData.fullName}
                onChange={handleChange}
              />
              {errors.fullName && <div className="form-error"><AlertCircle size={14} />{errors.fullName}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Email Address (Read-only)</label>
              <input
                type="email"
                className="form-input"
                value={profile?.email || ''}
                disabled
                style={{ opacity: 0.6, cursor: 'not-allowed' }}
              />
            </div>
          </div>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Phone Number</label>
              <input
                type="tel"
                name="phoneNumber"
                className="form-input"
                value={formData.phoneNumber}
                onChange={handleChange}
                placeholder="+94 77 123 4567"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Address</label>
              <input
                type="text"
                name="address"
                className="form-input"
                value={formData.address}
                onChange={handleChange}
                placeholder="Street Address, City"
              />
            </div>
          </div>

          {/* Recycler Details if role == Recycler */}
          {profile?.role === 'Recycler' && (
            <div style={{ marginTop: '1.5rem', padding: '1.5rem', background: 'rgba(0,0,0,0.25)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)' }}>
              <h3 style={{ fontSize: '1.05rem', fontWeight: 700, marginBottom: '1.25rem', color: 'var(--accent)' }}>
                Recycling Facility Profile
              </h3>

              <div className="form-group">
                <label className="form-label">Company Name</label>
                <input
                  type="text"
                  name="companyName"
                  className="form-input"
                  value={formData.companyName}
                  onChange={handleChange}
                />
              </div>

              <div className="grid-2">
                <div className="form-group">
                  <label className="form-label">Registration # (Read-only)</label>
                  <input
                    type="text"
                    className="form-input"
                    value={profile?.recyclerProfile?.businessRegistrationNumber || ''}
                    disabled
                    style={{ opacity: 0.6, cursor: 'not-allowed' }}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Capacity (Kg/month)</label>
                  <input
                    type="number"
                    name="operationalCapacityKg"
                    className="form-input"
                    value={formData.operationalCapacityKg}
                    onChange={handleChange}
                  />
                </div>
              </div>

              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Facility Plant Address</label>
                <input
                  type="text"
                  name="facilityAddress"
                  className="form-input"
                  value={formData.facilityAddress}
                  onChange={handleChange}
                />
              </div>
            </div>
          )}

          <div style={{ marginTop: '2rem', display: 'flex', justifyContent: 'flex-end', gap: '1rem' }}>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={loadProfile}
              disabled={saving}
            >
              Reset Changes
            </button>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={saving}
            >
              <Save size={18} />
              {saving ? 'Saving...' : 'Save Profile'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
