import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../services/api';
import { Leaf, Mail, AlertCircle, CheckCircle, ArrowRight } from 'lucide-react';

export const ForgotPasswordView = () => {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    setEmail(e.target.value);
    setError('');
  };

  const validate = () => {
    if (!email.trim()) {
      setError('Email is required.');
      return false;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setError('Please enter a valid email address.');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccessMessage('');
    if (!validate()) return;

    setLoading(true);
    try {
      const response = await authApi.forgotPassword({ email: email.trim() });
      setSuccessMessage(response.data.message || 'Check your email for reset instructions.');
    } catch (err) {
      // Even on error, don't reveal whether the email exists — show the same generic message
      setSuccessMessage('Check your email for reset instructions.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '480px', margin: '3rem auto' }}>
      <div className="glass-card">
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
            <Leaf size={32} style={{ color: 'var(--primary)' }} />
          </div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            Forgot Password?
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>
            Enter your email and we'll send you reset instructions
          </p>
        </div>

        {successMessage && (
          <div className="alert alert-success">
            <CheckCircle size={18} />
            <span>{successMessage}</span>
          </div>
        )}

        {!successMessage && (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label">Email Address</label>
              <input
                type="email"
                name="email"
                className="form-input"
                placeholder="akini@ecotrack.lk"
                value={email}
                onChange={handleChange}
                autoComplete="email"
              />
              {error && <div className="form-error"><AlertCircle size={14} />{error}</div>}
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', padding: '0.9rem', fontSize: '1rem', marginTop: '1rem' }}
              disabled={loading}
            >
              {loading ? 'Sending...' : 'Send Reset Link'}
              <ArrowRight size={18} />
            </button>
          </form>
        )}

        <div style={{ textAlign: 'center', marginTop: '1.75rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
          Remembered your password?{' '}
          <Link to="/login" style={{ color: 'var(--primary)', fontWeight: 600 }}>
            Back to Login
          </Link>
        </div>
      </div>
    </div>
  );
};