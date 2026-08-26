import React, { useState } from 'react';
import { authApi } from '../services/api';
import { Leaf, User, Building2, AlertCircle, CheckCircle2, ArrowRight } from 'lucide-react';

export const RegisterView = () => {
  const [role, setRole] = useState('User'); // 'User' or 'Recycler'
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '',
    address: '',
    companyName: '',
    businessRegistrationNumber: '',
    facilityAddress: '',
    operationalCapacityKg: ''
  });

  const [errors, setErrors] = useState({});
  const [serverError, setServerError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const validateForm = () => {
    const newErrors = {};

    if (!formData.fullName.trim()) {
      newErrors.fullName = 'Full name is required.';
    } else if (formData.fullName.trim().length < 2) {
      newErrors.fullName = 'Full name must be at least 2 characters.';
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email.trim())) {
      newErrors.email = 'Please enter a valid email address.';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required.';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters.';
    }

    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match.';
    }

    if (role === 'Recycler') {
      if (!formData.companyName.trim()) {
        newErrors.companyName = 'Company name is required for recyclers.';
      }
      if (!formData.businessRegistrationNumber.trim()) {
        newErrors.businessRegistrationNumber = 'Business registration number is required.';
      }
      if (!formData.facilityAddress.trim()) {
        newErrors.facilityAddress = 'Facility address is required.';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: '' }));
    }
    setServerError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setLoading(true);
    setServerError('');
    setSuccessMessage('');

    const payload = {
      fullName: formData.fullName.trim(),
      email: formData.email.trim().toLowerCase(),
      password: formData.password,
      role: role,
      phoneNumber: formData.phoneNumber.trim() || null,
      address: formData.address.trim() || null,
      ...(role === 'Recycler' ? {
        companyName: formData.companyName.trim(),
        businessRegistrationNumber: formData.businessRegistrationNumber.trim(),
        facilityAddress: formData.facilityAddress.trim(),
        operationalCapacityKg: formData.operationalCapacityKg ? parseFloat(formData.operationalCapacityKg) : 0
      } : {})
    };

    try {
      const response = await authApi.register(payload);
      setSuccessMessage(response.data.message || 'Account created successfully. Please log in.');
    } catch (error) {
      const msg = error.response?.data?.message || 'Registration failed. Please check your inputs.';
      setServerError(msg);
      if (error.response?.data?.errors) {
        setErrors(prev => ({ ...prev, ...error.response.data.errors }));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '640px', margin: '2rem auto' }}>
      <div className="glass-card">
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
            <Leaf size={32} style={{ color: 'var(--primary)' }} />
          </div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            Create Your Account
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>
            Join the EcoTrack circular economy and e-waste management network
          </p>
        </div>

        {/* User Role Selection Tabs */}
        <div className="tabs">
          <button
            type="button"
            className={`tab-btn ${role === 'User' ? 'active' : ''}`}
            onClick={() => { setRole('User'); setServerError(''); }}
          >
            <User size={16} style={{ display: 'inline', marginRight: '6px', verticalAlign: 'middle' }} />
            Individual / Corporate User
          </button>
          <button
            type="button"
            className={`tab-btn ${role === 'Recycler' ? 'active' : ''}`}
            onClick={() => { setRole('Recycler'); setServerError(''); }}
          >
            <Building2 size={16} style={{ display: 'inline', marginRight: '6px', verticalAlign: 'middle' }} />
            Certified Recycler
          </button>
        </div>

        {role === 'Recycler' && (
          <div className="alert alert-info">
            <AlertCircle size={18} />
            <div>
              <strong>Recycler Verification:</strong> Recycler accounts are subject to administrative license verification and will be initialized in <strong>Pending</strong> status upon registration.
            </div>
          </div>
        )}

        {serverError && (
          <div className="alert alert-danger">
            <AlertCircle size={18} />
            <span>{serverError}</span>
          </div>
        )}

        {successMessage && (
          <div className="alert alert-success">
            <CheckCircle2 size={18} />
            <div>
              <strong>Success:</strong> {successMessage}
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">Full Name *</label>
            <input
              type="text"
              name="fullName"
              className="form-input"
              placeholder="e.g. Akini Panapitiya"
              value={formData.fullName}
              onChange={handleChange}
            />
            {errors.fullName && <div className="form-error"><AlertCircle size={14} />{errors.fullName}</div>}
          </div>

          <div className="form-group">
            <label className="form-label">Email Address *</label>
            <input
              type="email"
              name="email"
              className="form-input"
              placeholder="akini@ecotrack.lk"
              value={formData.email}
              onChange={handleChange}
            />
            {errors.email && <div className="form-error"><AlertCircle size={14} />{errors.email}</div>}
          </div>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Password * (min 8 chars)</label>
              <input
                type="password"
                name="password"
                className="form-input"
                placeholder="••••••••"
                value={formData.password}
                onChange={handleChange}
              />
              {errors.password && <div className="form-error"><AlertCircle size={14} />{errors.password}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Confirm Password *</label>
              <input
                type="password"
                name="confirmPassword"
                className="form-input"
                placeholder="••••••••"
                value={formData.confirmPassword}
                onChange={handleChange}
              />
              {errors.confirmPassword && <div className="form-error"><AlertCircle size={14} />{errors.confirmPassword}</div>}
            </div>
          </div>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Phone Number</label>
              <input
                type="tel"
                name="phoneNumber"
                className="form-input"
                placeholder="+94 77 123 4567"
                value={formData.phoneNumber}
                onChange={handleChange}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Address</label>
              <input
                type="text"
                name="address"
                className="form-input"
                placeholder="Colombo, Sri Lanka"
                value={formData.address}
                onChange={handleChange}
              />
            </div>
          </div>

          {/* Recycler Specific Fields */}
          {role === 'Recycler' && (
            <div style={{ padding: '1.25rem', background: 'rgba(0,0,0,0.25)', borderRadius: 'var(--radius-md)', border: '1px solid var(--border-color)', marginBottom: '1.5rem' }}>
              <h3 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '1rem', color: 'var(--accent)' }}>
                Recycling Facility Details
              </h3>

              <div className="form-group">
                <label className="form-label">Company / Facility Name *</label>
                <input
                  type="text"
                  name="companyName"
                  className="form-input"
                  placeholder="Green Yard E-Waste Solutions Ltd"
                  value={formData.companyName}
                  onChange={handleChange}
                />
                {errors.companyName && <div className="form-error"><AlertCircle size={14} />{errors.companyName}</div>}
              </div>

              <div className="grid-2">
                <div className="form-group">
                  <label className="form-label">Business Registration # *</label>
                  <input
                    type="text"
                    name="businessRegistrationNumber"
                    className="form-input"
                    placeholder="PV-1029384"
                    value={formData.businessRegistrationNumber}
                    onChange={handleChange}
                  />
                  {errors.businessRegistrationNumber && <div className="form-error"><AlertCircle size={14} />{errors.businessRegistrationNumber}</div>}
                </div>

                <div className="form-group">
                  <label className="form-label">Capacity (Kg/month)</label>
                  <input
                    type="number"
                    name="operationalCapacityKg"
                    className="form-input"
                    placeholder="5000"
                    value={formData.operationalCapacityKg}
                    onChange={handleChange}
                  />
                </div>
              </div>

              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Facility Plant Address *</label>
                <input
                  type="text"
                  name="facilityAddress"
                  className="form-input"
                  placeholder="Zone 4, Industrial Park, Kaduwela"
                  value={formData.facilityAddress}
                  onChange={handleChange}
                />
                {errors.facilityAddress && <div className="form-error"><AlertCircle size={14} />{errors.facilityAddress}</div>}
              </div>
            </div>
          )}

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', padding: '0.9rem', fontSize: '1rem' }}
            disabled={loading}
          >
            {loading ? 'Creating Account...' : `Register as ${role}`}
            <ArrowRight size={18} />
          </button>
        </form>
      </div>
    </div>
  );
};
