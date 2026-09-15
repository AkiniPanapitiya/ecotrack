import React, { useCallback, useEffect, useState } from 'react';
import { auditApi } from '../services/api';
import { FileText, RefreshCw, ShieldAlert } from 'lucide-react';

const actionLabels = {
  REGISTER: 'Registration',
  LOGIN_SUCCESS: 'Login Success',
  LOGIN_FAILED: 'Login Failed',
};

const actionColors = {
  REGISTER: 'var(--accent)',
  LOGIN_SUCCESS: 'var(--primary)',
  LOGIN_FAILED: 'var(--danger)',
};

export const AuditReportView = () => {
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadReport = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const response = await auditApi.getReport({ limit: 50 });
      setReport(response.data);
    } catch (err) {
      console.error('Failed to load audit report:', err);
      setError('Could not load the audit report. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadReport();
  }, [loadReport]);

  const stats = [
    { label: 'Total Events', value: report?.totalEvents },
    { label: 'Registrations', value: report?.registrationCount },
    { label: 'Successful Logins', value: report?.successfulLogins },
    { label: 'Failed Logins', value: report?.failedLogins },
  ];

  return (
    <div style={{ maxWidth: '1100px', margin: '1rem auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.4rem' }}>Audit Report</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
            Registration and login activity recorded across EcoTrack.
          </p>
        </div>
        <button className="btn btn-secondary" onClick={loadReport} disabled={loading}>
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <div className="alert alert-info">
        <ShieldAlert size={18} />
        <div>
          This report is currently visible to all logged-in accounts. Restricting it to an Admin-only
          role, plus filtering and full history browsing, is planned for a future sprint.
        </div>
      </div>

      {loading && <p style={{ color: 'var(--text-secondary)' }}>Loading audit report...</p>}

      {!loading && error && <p style={{ color: 'var(--danger)' }}>{error}</p>}

      {!loading && !error && report && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1.25rem', marginBottom: '2rem' }}>
            {stats.map((stat) => (
              <div className="glass-card" key={stat.label} style={{ padding: '1.25rem' }}>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', marginBottom: '0.4rem' }}>{stat.label}</p>
                <p style={{ fontSize: '1.75rem', fontWeight: 800 }}>{stat.value ?? 0}</p>
              </div>
            ))}
          </div>

          <div className="glass-card">
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '1.25rem' }}>
              <FileText size={18} style={{ color: 'var(--warning)' }} />
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700 }}>Recent Activity</h3>
            </div>

            {report.logs.length === 0 && (
              <p style={{ color: 'var(--text-secondary)' }}>
                No audit events recorded yet. Events are logged from the moment this feature was enabled,
                so earlier activity won't appear here.
              </p>
            )}

            {report.logs.length > 0 && (
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Timestamp</th>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Action</th>
                      <th>IP Address</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.logs.map((log) => (
                      <tr key={log.id}>
                        <td>{new Date(log.timestamp).toLocaleString()}</td>
                        <td>{log.userEmail}</td>
                        <td>{log.role}</td>
                        <td>
                          <span style={{ color: actionColors[log.action] || 'var(--text-primary)', fontWeight: 600 }}>
                            {actionLabels[log.action] || log.action}
                          </span>
                        </td>
                        <td>{log.ipAddress || '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default AuditReportView;