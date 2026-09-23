import React from 'react';
import { Link } from 'react-router-dom';
import { ShieldX } from 'lucide-react';

export const AccessDeniedView = () => {
  return (
    <div
      style={{
        minHeight: '60vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        textAlign: 'center',
        padding: '2rem',
        background: 'var(--surface)',
      }}
    >
      <div
        style={{
          width: '80px',
          height: '80px',
          borderRadius: '50%',
          background: 'var(--surface-hover)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <ShieldX size={40} color="var(--text-secondary)" />
      </div>

      <h1
        style={{
          fontSize: '1.75rem',
          fontWeight: 700,
          color: 'var(--text-primary)',
          margin: '0 0 0.5rem 0',
        }}
      >
        Access Denied
      </h1>

      <p
        style={{
          fontSize: '1rem',
          color: 'var(--text-secondary)',
          maxWidth: '420px',
          lineHeight: 1.6,
          margin: '0 0 2rem 0',
        }}
      >
        You do not have permission to view this page. This area is restricted to users
        with the required role. If you believe this is an error, please contact an
        administrator.
      </p>

      <Link
        to="/dashboard"
        className="btn btn-primary"
        style={{ padding: '0.6rem 1.5rem', fontSize: '0.95rem' }}
      >
        Go to Dashboard
      </Link>
    </div>
  );
};

export default AccessDeniedView;
