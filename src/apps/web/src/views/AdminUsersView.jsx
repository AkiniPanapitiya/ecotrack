import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { getUsers, changeUserRole, toggleUserActive } from '../services/kycApi';
import { ChevronDown, Check, User, Truck, Shield, AlertTriangle, Users } from 'lucide-react';

const ROLES = [
  { value: 'User', label: 'User', icon: User },
  { value: 'Recycler', label: 'Recycler', icon: Truck },
  { value: 'Admin', label: 'Admin', icon: Shield },
];

const roleColors = {
  User: { bg: 'rgba(16,185,129,0.15)', border: 'rgba(16,185,129,0.3)', text: '#34d399' },
  Recycler: { bg: 'rgba(6,182,212,0.15)', border: 'rgba(6,182,212,0.3)', text: '#22d3ee' },
  Admin: { bg: 'rgba(239,68,68,0.15)', border: 'rgba(239,68,68,0.3)', text: '#f87171' },
};

function RoleDropdown({ value, onChange, disabled, isOwnAccount }) {
  const [open, setOpen] = useState(false);
  const current = ROLES.find(r => r.value === value) || ROLES[0];
  const Icon = current.icon;
  const colors = roleColors[value] || roleColors.User;

  return (
    <div style={{ position: 'relative', display: 'inline-block' }}>
      <button
        type="button"
        onClick={() => { if (!disabled) setOpen(!open); }}
        disabled={disabled}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.4rem',
          padding: '0.35rem 0.5rem',
          width: '110px',
          borderRadius: '6px',
          border: `1px solid ${colors.border}`,
          background: colors.bg,
          color: colors.text,
          fontWeight: 600,
          fontSize: '0.8rem',
          cursor: disabled ? 'not-allowed' : 'pointer',
          opacity: disabled ? 0.5 : 1,
          transition: 'all 0.15s ease',
          justifyContent: 'center'
        }}
      >
        <Icon size={14} />
        <span>{value}</span>
        <ChevronDown size={14} style={{ opacity: 0.6 }} />
      </button>

      {open && (
        <>
          <div style={{ position: 'fixed', inset: 0, zIndex: 999 }} onClick={() => setOpen(false)} />
          <div
            style={{
              position: 'absolute',
              top: 'calc(100% + 4px)',
              left: 0,
              width: '110px',
              background: 'rgba(30,41,59,0.95)',
              border: '1px solid rgba(16,185,129,0.3)',
              borderRadius: '8px',
              boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
              zIndex: 1000,
              overflow: 'hidden',
              padding: '0.25rem',
              animation: 'fadeIn 0.12s ease-out'
            }}
          >
            {ROLES.map(r => {
              const Icon = r.icon;
              const selected = r.value === value;
              const isSelfDemotion = isOwnAccount && r.value !== 'Admin';
              const optionDisabled = isSelfDemotion;

              return (
                <button
                  key={r.value}
                  type="button"
                  onClick={() => {
                    if (!optionDisabled) onChange(r.value);
                    setOpen(false);
                  }}
                  disabled={optionDisabled || disabled}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.5rem',
                    width: '100%',
                    padding: '0.45rem 0.4rem',
                    border: 'none',
                    background: selected ? 'rgba(16,185,129,0.12)' : optionDisabled ? 'rgba(239,68,68,0.08)' : 'rgba(255,255,255,0.03)',
                    color: selected ? '#34d399' : optionDisabled ? '#f87171' : '#f9fafb',
                    fontSize: '0.8rem',
                    fontWeight: selected ? 600 : 500,
                    cursor: optionDisabled ? 'not-allowed' : 'pointer',
                    textAlign: 'left',
                    borderRadius: '4px',
                    transition: 'background 0.1s ease',
                    opacity: optionDisabled ? 0.45 : 1
                  }}
                  onMouseEnter={e => { if (!optionDisabled && !selected) e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; }}
                  onMouseLeave={e => { if (!optionDisabled && !selected) e.currentTarget.style.background = 'rgba(255,255,255,0.03)'; }}
                >
                  <Icon size={14} style={{ opacity: 0.7 }} />
                  <span>{r.label}</span>
                  {optionDisabled && <AlertTriangle size={12} style={{ marginLeft: 'auto', opacity: 0.7, color: '#f87171' }} />}
                  {selected && <Check size={13} style={{ marginLeft: 'auto', color: '#34d399', opacity: 0.9 }} />}
                </button>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}

export const AdminUsersView = () => {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [savingUserId, setSavingUserId] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [selectedUser, setSelectedUser] = useState(null);
  const [confirmingChange, setConfirmingChange] = useState(null);
  const [confirmingToggle, setConfirmingToggle] = useState(null);
  const [roleFilter, setRoleFilter] = useState(null);
  const [statusFilter, setStatusFilter] = useState(null);
  const [openRoleFilter, setOpenRoleFilter] = useState(false);
  const [openStatusFilter, setOpenStatusFilter] = useState(false);
  const [sortField, setSortField] = useState(null);
  const [sortDir, setSortDir] = useState('DESC');

  const fetchUsers = async () => {
    try {
      const params = {};
      if (roleFilter) params.role = roleFilter;
      if (statusFilter !== null) params.status = statusFilter;
      if (sortField) { params.sortBy = sortField; params.sortDesc = sortDir === 'DESC'; }
      const res = await getUsers(params);
      setUsers(res.data.users || []);
    } catch (err) {
      setError('Failed to load users. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, [roleFilter, statusFilter, sortField, sortDir]);

  const doRoleChange = async (userId, newRole) => {
    setSavingUserId(userId);
    setMessage('');
    setError('');
    try {
      const res = await changeUserRole(userId, { newRole });
      setMessage(res.data.message);
      setUsers(prev =>
        prev.map(u =>
          u.id === userId ? { ...u, role: newRole } : u
        )
      );
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to change role.');
    } finally {
      setSavingUserId(null);
    }
  };

  const handleRoleChange = (userId, newRole, userName, currentRole) => {
    const isAdminChange = (currentRole === 'Admin' && newRole !== 'Admin') ||
                         (currentRole !== 'Admin' && newRole === 'Admin');

    if (isAdminChange && String(userId) !== String(user?.userId)) {
      setConfirmingChange({ userId, name: userName, currentRole, newRole });
      return;
    }
    doRoleChange(userId, newRole);
  };

  const confirmChange = () => {
    if (!confirmingChange) return;
    const { userId, newRole } = confirmingChange;
    setConfirmingChange(null);
    doRoleChange(userId, newRole);
  };

  const confirmToggle = async () => {
    if (!confirmingToggle) return;
    const { userId, newStatus } = confirmingToggle;
    setConfirmingToggle(null);
    try {
      await toggleUserActive(userId, newStatus);
      setMessage(newStatus ? 'User activated successfully.' : 'User deactivated successfully.');
      setUsers(prev => prev.map(u => u.id === userId ? { ...u, isActive: newStatus } : u));
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update active status.');
    }
  };

  const openUserModal = (u) => {
    const roleColorsMap = { User: '#34d399', Recycler: '#22d3ee', Admin: '#f87171' };
    setSelectedUser({ ...u, roleColor: roleColorsMap[u.role] || '#9ca3af' });
  };
  const closeUserModal = () => setSelectedUser(null);

  if (loading) {
    return (
      <div className="glass-card" style={{ padding: '2rem', textAlign: 'center' }}>
        <div className="spinner">Loading users...</div>
      </div>
    );
  }

  return (
    <div style={{ padding: '1.5rem' }}>
      <h2 style={{ marginBottom: '1rem', color: 'var(--text-primary)' }}>
        User Management
      </h2>

      {/* Filter bar */}
      <div style={{
        display: 'flex',
        gap: '0.75rem',
        marginBottom: '1.25rem',
        flexWrap: 'wrap',
        alignItems: 'center'
      }}>
        {/* Role filter */}
        <div style={{ position: 'relative', display: 'inline-block' }}>
          <button
            type="button"
            onClick={() => setOpenRoleFilter(!openRoleFilter)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.4rem',
              padding: '0.4rem 0.75rem',
              borderRadius: '6px',
              border: '1px solid var(--border-color)',
              background: 'rgba(255,255,255,0.04)',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              fontSize: '0.8rem',
              cursor: 'pointer',
              transition: 'all 0.15s ease'
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.08)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.04)'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
          >
            <Shield size={13} />
            <span>Role: {roleFilter || 'All'}</span>
            <ChevronDown size={13} style={{ opacity: 0.5 }} />
          </button>
          {openRoleFilter && (
            <>
              <div style={{ position: 'fixed', inset: 0, zIndex: 999 }} onClick={() => setOpenRoleFilter(false)} />
              <div style={{
                position: 'absolute',
                top: 'calc(100% + 4px)',
                left: 0,
                minWidth: '120px',
                background: 'rgba(30,41,59,0.95)',
                border: '1px solid var(--border-color)',
                borderRadius: '8px',
                boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
                zIndex: 1000,
                padding: '0.25rem',
                animation: 'fadeIn 0.12s ease-out'
              }}>
                <button onClick={() => { setRoleFilter(null); setOpenRoleFilter(false); }} style={{ width: '100%', padding: '0.4rem 0.6rem', border: 'none', background: !roleFilter ? 'rgba(16,185,129,0.12)' : 'transparent', color: !roleFilter ? '#34d399' : '#f9fafb', fontSize: '0.8rem', fontWeight: 500, cursor: 'pointer', textAlign: 'left', borderRadius: '4px' }}>All Roles</button>
                {ROLES.map(r => (
                  <button key={r.value} onClick={() => { setRoleFilter(r.value); setOpenRoleFilter(false); }} style={{ width: '100%', padding: '0.4rem 0.6rem', border: 'none', background: roleFilter === r.value ? 'rgba(16,185,129,0.12)' : 'transparent', color: roleFilter === r.value ? '#34d399' : '#f9fafb', fontSize: '0.8rem', fontWeight: 500, cursor: 'pointer', textAlign: 'left', borderRadius: '4px' }}>
                    <r.icon size={12} style={{ marginRight: '0.3rem', opacity: 0.7 }} />
                    {r.label}
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        {/* Status filter */}
        <div style={{ position: 'relative', display: 'inline-block' }}>
          <button
            type="button"
            onClick={() => setOpenStatusFilter(!openStatusFilter)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.4rem',
              padding: '0.4rem 0.75rem',
              borderRadius: '6px',
              border: '1px solid var(--border-color)',
              background: 'rgba(255,255,255,0.04)',
              color: 'var(--text-secondary)',
              fontWeight: 500,
              fontSize: '0.8rem',
              cursor: 'pointer',
              transition: 'all 0.15s ease'
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.08)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.04)'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
          >
            <Users size={13} />
            <span>Status: {statusFilter === null ? 'All' : statusFilter ? 'Active' : 'Inactive'}</span>
            <ChevronDown size={13} style={{ opacity: 0.5 }} />
          </button>
          {openStatusFilter && (
            <>
              <div style={{ position: 'fixed', inset: 0, zIndex: 999 }} onClick={() => setOpenStatusFilter(false)} />
              <div style={{
                position: 'absolute',
                top: 'calc(100% + 4px)',
                left: 0,
                minWidth: '120px',
                background: 'rgba(30,41,59,0.95)',
                border: '1px solid var(--border-color)',
                borderRadius: '8px',
                boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
                zIndex: 1000,
                padding: '0.25rem',
                animation: 'fadeIn 0.12s ease-out'
              }}>
                <button onClick={() => { setStatusFilter(null); setOpenStatusFilter(false); }} style={{ width: '100%', padding: '0.4rem 0.6rem', border: 'none', background: statusFilter === null ? 'rgba(16,185,129,0.12)' : 'transparent', color: statusFilter === null ? '#34d399' : '#f9fafb', fontSize: '0.8rem', fontWeight: 500, cursor: 'pointer', textAlign: 'left', borderRadius: '4px' }}>All Status</button>
                <button onClick={() => { setStatusFilter(true); setOpenStatusFilter(false); }} style={{ width: '100%', padding: '0.4rem 0.6rem', border: 'none', background: statusFilter === true ? 'rgba(16,185,129,0.12)' : 'transparent', color: statusFilter === true ? '#34d399' : '#f9fafb', fontSize: '0.8rem', fontWeight: 500, cursor: 'pointer', textAlign: 'left', borderRadius: '4px' }}>
                  <span style={{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', background: '#34d399', marginRight: '0.4rem' }} />
                  Active
                </button>
                <button onClick={() => { setStatusFilter(false); setOpenStatusFilter(false); }} style={{ width: '100%', padding: '0.4rem 0.6rem', border: 'none', background: statusFilter === false ? 'rgba(239,68,68,0.12)' : 'transparent', color: statusFilter === false ? '#f87171' : '#f9fafb', fontSize: '0.8rem', fontWeight: 500, cursor: 'pointer', textAlign: 'left', borderRadius: '4px' }}>
                  <span style={{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', background: '#f87171', marginRight: '0.4rem' }} />
                  Inactive
                </button>
              </div>
            </>
          )}
        </div>

        {/* Clear filters */}
        {(roleFilter || statusFilter !== null) && (
          <button
            className="btn btn-ghost"
            style={{
              padding: '0.4rem 0.6rem',
              background: 'transparent',
              border: 'none',
              color: 'var(--text-secondary)',
              fontSize: '0.8rem',
              cursor: 'pointer',
              borderRadius: '6px',
              transition: 'all 0.15s ease'
            }}
            onClick={() => { setRoleFilter(null); setStatusFilter(null); }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
          >
            Clear filters
          </button>
        )}

        {/* Results count */}
        <span style={{ color: 'var(--text-secondary)', fontSize: '0.8rem', marginLeft: 'auto', fontWeight: 500 }}>
          {users.length} user{users.length !== 1 ? 's' : ''}
        </span>
      </div>

      {message && (
        <div className="alert alert-success" style={{ marginBottom: '1rem' }}>
          {message}
        </div>
      )}

      {error && (
        <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
          {error}
        </div>
      )}

      {users.length === 0 ? (
        <div className="glass-card" style={{ padding: '2rem', textAlign: 'center' }}>
          No users found.
        </div>
      ) : (
        <div style={{ overflowX: 'auto', overflowY: 'auto', maxHeight: 'calc(100vh - 280px)', display: 'block' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '700px' }}>
            <thead style={{ position: 'sticky', top: 0, zIndex: 10 }}>
              <tr style={{ background: '#111827' }}>
                <th style={cellStyle}>Name</th>
                <th style={cellStyle}>Email</th>
                <th style={cellStyle}>Role</th>
                <th style={cellStyle}>Status</th>
                <th
                  style={{ ...cellStyle, cursor: 'pointer', userSelect: 'none' }}
                  onClick={() => {
                    if (sortField === 'Joined') {
                      setSortDir(prev => prev === 'DESC' ? 'ASC' : 'DESC');
                    } else {
                      setSortField('Joined');
                      setSortDir('DESC');
                    }
                  }}
                >
                  Joined {sortField === 'Joined' ? (sortDir === 'ASC' ? '▲' : '▼') : ''}
                </th>
                <th style={{ ...cellStyle, width: '140px' }}>Action</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u, idx) => (
                <tr key={u.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.12)', transition: 'background 0.15s ease' }}>
                  <td style={cellStyle}>{u.fullName || '—'}</td>
                  <td style={cellStyle}>{u.email}</td>
                  <td style={cellStyle}>
                    {savingUserId === u.id ? (
                      <span className="spinner" style={{ fontSize: '0.8rem', display: 'inline-block', verticalAlign: 'middle' }}>Saving...</span>
                    ) : (
                      <RoleDropdown
                        value={u.role}
                        onChange={(newRole) => handleRoleChange(u.id, newRole, u.fullName, u.role)}
                        disabled={savingUserId !== null && savingUserId !== u.id}
                        isOwnAccount={String(user?.userId) === String(u.id)}
                      />
                    )}
                  </td>
                  <td style={cellStyle}>
                    <span style={{
                      fontWeight: 600,
                      fontSize: '0.75rem',
                      padding: '0.2rem 0.5rem',
                      borderRadius: '4px',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.35rem',
                      background: u.isActive ? 'rgba(16,185,129,0.08)' : 'rgba(239,68,68,0.08)',
                      border: '1px solid ' + (u.isActive ? 'rgba(16,185,129,0.25)' : 'rgba(239,68,68,0.25)'),
                      color: u.isActive ? '#34d399' : '#f87171'
                    }}>
                      <span style={{ width: '6px', height: '6px', borderRadius: '50%', background: u.isActive ? '#34d399' : '#f87171', display: 'inline-block' }} />
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td style={cellStyle}>
                    {new Date(u.createdAt).toLocaleDateString()}
                  </td>
                  <td style={cellStyle}>
                    <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'center' }}>
                      <button
                        className="btn btn-secondary"
                        style={{
                          flex: '0 0 auto',
                          width: '72px',
                          padding: '0.35rem 0.5rem',
                          fontSize: '0.8rem',
                          background: 'rgba(255,255,255,0.06)',
                          border: '1px solid var(--border-color)',
                          color: 'var(--text-primary)',
                          borderRadius: '6px',
                          cursor: 'pointer',
                          transition: 'all 0.15s ease',
                          fontWeight: 500
                        }}
                        onClick={() => openUserModal(u)}
                        title="View user details"
                        onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.1)'; e.currentTarget.style.borderColor = 'rgba(255,255,255,0.15)'; }}
                        onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.borderColor = 'var(--border-color)'; }}
                      >
                        View
                      </button>
                      {String(u.id) !== String(user?.userId) && (
                        <button
                          className="btn btn-ghost"
                          style={{
                            flex: '0 0 auto',
                            width: '88px',
                            padding: '0.35rem 0.5rem',
                            fontSize: '0.8rem',
                            background: u.isActive ? 'transparent' : 'rgba(16,185,129,0.08)',
                            border: `1px solid ${u.isActive ? 'rgba(239,68,68,0.35)' : 'rgba(16,185,129,0.35)'}`,
                            color: u.isActive ? '#f87171' : '#34d399',
                            borderRadius: '6px',
                            cursor: 'pointer',
                            transition: 'all 0.15s ease',
                            fontWeight: 600,
                            whiteSpace: 'nowrap'
                          }}
                          onClick={() => setConfirmingToggle({ userId: u.id, newStatus: !u.isActive, name: u.fullName, email: u.email })}
                          title={u.isActive ? 'Deactivate this user' : 'Activate this user'}
                          onMouseEnter={e => { e.currentTarget.style.opacity = '0.85'; }}
                          onMouseLeave={e => { e.currentTarget.style.opacity = '1'; }}
                        >
                          {u.isActive ? 'Deactivate' : 'Activate'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* User Detail Modal */}
      {selectedUser && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.6)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            backdropFilter: 'blur(4px)',
            animation: 'fadeIn 0.15s ease-out'
          }}
          onClick={closeUserModal}
        >
          <div
            className="glass-card"
            style={{
              width: '440px',
              maxWidth: '90vw',
              padding: '0',
              borderRadius: '16px',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
              overflow: 'hidden',
              position: 'relative',
              animation: 'fadeIn 0.2s ease-out'
            }}
            onClick={e => e.stopPropagation()}
          >
            {/* Header */}
            <div style={{
              padding: '1rem 1.5rem',
              background: 'rgba(16,185,129,0.08)',
              borderBottom: '1px solid var(--border-color)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                <div style={{
                  width: '36px',
                  height: '36px',
                  borderRadius: '50%',
                  background: `radial-gradient(circle at 30% 30%, ${selectedUser.roleColor}44, ${selectedUser.roleColor}22)`,
                  border: `1px solid ${selectedUser.roleColor}44`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}>
                  <Users size={18} color={selectedUser.roleColor} />
                </div>
                <div>
                  <h3 style={{ margin: 0, color: 'var(--text-primary)', fontSize: '1.05rem', fontWeight: 600 }}>
                    User Details
                  </h3>
                  <p style={{ margin: 0, color: 'var(--text-secondary)', fontSize: '0.78rem' }}>
                    {selectedUser.email}
                  </p>
                </div>
              </div>
              <button
                className="btn btn-ghost"
                style={{
                  background: 'transparent',
                  border: 'none',
                  fontSize: '1.3rem',
                  cursor: 'pointer',
                  color: 'var(--text-secondary)',
                  padding: '0.25rem 0.5rem',
                  borderRadius: '6px',
                  transition: 'all 0.15s ease'
                }}
                onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
                onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
                onClick={closeUserModal}
                title="Close"
              >
                &times;
              </button>
            </div>

            {/* Body */}
            <div style={{ padding: '0.25rem 1.5rem 1.5rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
                <DetailRow label="Name" value={selectedUser.fullName || '—'} />
                <DetailRow label="Email" value={selectedUser.email || '—'} />
                <DetailRow
                  label="Role"
                  value={selectedUser.role || '—'}
                  roleColor={selectedUser.roleColor}
                />
                <DetailRow
                  label="Status"
                  value={selectedUser.isActive ? 'Active' : 'Inactive'}
                  status={selectedUser.isActive}
                />
                <DetailRow label="Joined" value={new Date(selectedUser.createdAt).toLocaleDateString()} />
                <DetailRow
                  label="ID"
                  value={selectedUser.id}
                  showFull
                />
              </div>

              {/* Footer action */}
              <button
                className="btn btn-secondary"
                style={{
                  width: '100%',
                  padding: '0.6rem',
                  marginTop: '1.25rem',
                  background: 'rgba(255,255,255,0.06)',
                  border: '1px solid var(--border-color)',
                  color: 'var(--text-primary)',
                  borderRadius: '8px',
                  fontWeight: 500,
                  fontSize: '0.875rem',
                  cursor: 'pointer',
                  transition: 'all 0.15s ease'
                }}
                onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.1)'; e.currentTarget.style.borderColor = 'rgba(255,255,255,0.15)'; }}
                onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.borderColor = 'var(--border-color)'; }}
                onClick={closeUserModal}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Role Change Confirmation Dialog */}
      {confirmingChange && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.6)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 2000,
            backdropFilter: 'blur(4px)',
            animation: 'fadeIn 0.15s ease-out'
          }}
          onClick={() => setConfirmingChange(null)}
        >
          <div
            className="glass-card"
            style={{
              width: '440px',
              maxWidth: '90vw',
              padding: '0',
              borderRadius: '16px',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
              overflow: 'hidden',
              position: 'relative',
              animation: 'fadeIn 0.2s ease-out'
            }}
            onClick={e => e.stopPropagation()}
          >
            <button
              className="btn btn-ghost"
              style={{
                position: 'absolute',
                top: '0.75rem',
                right: '0.75rem',
                background: 'transparent',
                border: 'none',
                fontSize: '1.3rem',
                cursor: 'pointer',
                color: 'var(--text-secondary)',
                padding: '0.25rem 0.5rem',
                borderRadius: '6px',
                transition: 'all 0.15s ease'
              }}
              onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
              onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
              onClick={() => setConfirmingChange(null)}
            >
              &times;
            </button>

            <div style={{ padding: '1.5rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.25rem' }}>
                <div style={{
                  width: '44px',
                  height: '44px',
                  borderRadius: '10px',
                  background: 'rgba(245,158,11,0.15)',
                  border: '1px solid rgba(245,158,11,0.3)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}>
                  <AlertTriangle size={22} color="#f59e0b" />
                </div>
                <div>
                  <h3 style={{ margin: 0, color: 'var(--text-primary)', fontSize: '1.05rem', fontWeight: 600 }}>
                    Confirm Role Change
                  </h3>
                </div>
              </div>

              <div style={{
                background: 'rgba(255,255,255,0.03)',
                border: '1px solid var(--border-color)',
                borderRadius: '10px',
                padding: '1rem',
                marginBottom: '1.25rem'
              }}>
                <p style={{ color: 'var(--text-primary)', margin: '0 0 0.5rem 0', fontSize: '0.9rem', lineHeight: 1.6 }}>
                  Changing <strong>{confirmingChange.name}</strong>'s role from
                </p>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.6rem', margin: '0.5rem 0' }}>
                  <span style={{
                    padding: '0.3rem 0.8rem',
                    borderRadius: '6px',
                    background: roleColors[confirmingChange.currentRole]?.bg || 'rgba(156,163,175,0.15)',
                    border: `1px solid ${roleColors[confirmingChange.currentRole]?.border || 'rgba(156,163,175,0.3)'}`,
                    color: roleColors[confirmingChange.currentRole]?.text || '#9ca3af',
                    fontWeight: 600,
                    fontSize: '0.85rem'
                  }}>
                    {confirmingChange.currentRole}
                  </span>
                  <span style={{ color: '#6b7280', fontSize: '1.1rem', fontWeight: 300 }}>→</span>
                  <span style={{
                    padding: '0.3rem 0.8rem',
                    borderRadius: '6px',
                    background: roleColors[confirmingChange.newRole]?.bg || 'rgba(156,163,175,0.15)',
                    border: `1px solid ${roleColors[confirmingChange.newRole]?.border || 'rgba(156,163,175,0.3)'}`,
                    color: roleColors[confirmingChange.newRole]?.text || '#9ca3af',
                    fontWeight: 600,
                    fontSize: '0.85rem'
                  }}>
                    {confirmingChange.newRole}
                  </span>
                </div>
                <p style={{ color: 'var(--text-secondary)', margin: '0.75rem 0 0 0', fontSize: '0.85rem', lineHeight: 1.6 }}>
                  {confirmingChange.currentRole === 'Admin' && confirmingChange.newRole !== 'Admin' ? (
                    <>
                      <strong>{confirmingChange.name}</strong> will lose Admin access immediately. They will no longer be able to view or manage admin-only pages.
                    </>
                  ) : confirmingChange.currentRole !== 'Admin' && confirmingChange.newRole === 'Admin' ? (
                    <>
                      <strong>{confirmingChange.name}</strong> will gain Admin access and be able to view and manage all admin-only pages.
                    </>
                  ) : null}
                </p>
              </div>

              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <button className="btn btn-secondary" style={{ flex: 1, padding: '0.6rem', background: 'rgba(255,255,255,0.06)', border: '1px solid var(--border-color)', color: 'var(--text-primary)', borderRadius: '8px', fontWeight: 500, fontSize: '0.875rem', cursor: 'pointer' }} onClick={() => setConfirmingChange(null)}>
                  Cancel
                </button>
                <button className="btn btn-secondary" style={{ flex: 1, padding: '0.6rem', background: 'rgba(16,185,129,0.12)', border: '1px solid rgba(16,185,129,0.4)', color: '#34d399', borderRadius: '8px', fontWeight: 600, fontSize: '0.875rem', cursor: 'pointer' }} onClick={confirmChange}>
                  Confirm Change
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Active Status Confirmation Dialog */}
      {confirmingToggle && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.6)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 2000,
            backdropFilter: 'blur(4px)',
            animation: 'fadeIn 0.15s ease-out'
          }}
          onClick={() => setConfirmingToggle(null)}
        >
          <div
            className="glass-card"
            style={{
              width: '400px',
              maxWidth: '90vw',
              padding: '0',
              borderRadius: '16px',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
              overflow: 'hidden',
              position: 'relative',
              animation: 'fadeIn 0.2s ease-out'
            }}
            onClick={e => e.stopPropagation()}
          >
            <button
              className="btn btn-ghost"
              style={{
                position: 'absolute',
                top: '0.75rem',
                right: '0.75rem',
                background: 'transparent',
                border: 'none',
                fontSize: '1.3rem',
                cursor: 'pointer',
                color: 'var(--text-secondary)',
                padding: '0.25rem 0.5rem',
                borderRadius: '6px',
                transition: 'all 0.15s ease'
              }}
              onMouseEnter={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.06)'; e.currentTarget.style.color = 'var(--text-primary)'; }}
              onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--text-secondary)'; }}
              onClick={() => setConfirmingToggle(null)}
            >
              &times;
            </button>
            <div style={{ padding: '1.5rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.25rem' }}>
                <div style={{
                  width: '44px',
                  height: '44px',
                  borderRadius: '10px',
                  background: confirmingToggle.newStatus ? 'rgba(16,185,129,0.15)' : 'rgba(239,68,68,0.15)',
                  border: `1px solid ${confirmingToggle.newStatus ? 'rgba(16,185,129,0.3)' : 'rgba(239,68,68,0.3)'}`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center'
                }}>
                  <Users size={22} color={confirmingToggle.newStatus ? '#34d399' : '#f87171'} />
                </div>
                <div>
                  <h3 style={{ margin: 0, color: 'var(--text-primary)', fontSize: '1.05rem', fontWeight: 600 }}>
                    {confirmingToggle.newStatus ? 'Activate User' : 'Deactivate User'}
                  </h3>
                </div>
              </div>
              <p style={{ color: 'var(--text-primary)', margin: '0 0 0.5rem 0', fontSize: '0.9rem' }}>
                You are about to <strong>{confirmingToggle.newStatus ? 'activate' : 'deactivate'}</strong>{' '}
                <strong>{confirmingToggle.name || 'this user'}</strong> ({confirmingToggle.email}).
              </p>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: '1.25rem', lineHeight: 1.6 }}>
                {confirmingToggle.newStatus
                  ? 'The user will be able to log in and access their role-based features again.'
                  : 'The user will no longer be able to log in. Their account will remain in the system but will be inactive.'}
              </p>
              <div style={{ display: 'flex', gap: '0.75rem' }}>
                <button className="btn btn-secondary" style={{ flex: 1, padding: '0.6rem', background: 'rgba(255,255,255,0.06)', border: '1px solid var(--border-color)', color: 'var(--text-primary)', borderRadius: '8px', fontWeight: 500, cursor: 'pointer' }} onClick={() => setConfirmingToggle(null)}>
                  Cancel
                </button>
                <button className="btn btn-secondary" style={{ flex: 1, padding: '0.6rem', background: confirmingToggle.newStatus ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)', border: `1px solid ${confirmingToggle.newStatus ? 'rgba(16,185,129,0.4)' : 'rgba(239,68,68,0.4)'}`, color: confirmingToggle.newStatus ? '#34d399' : '#f87171', borderRadius: '8px', fontWeight: 600, cursor: 'pointer' }} onClick={confirmToggle}>
                  {confirmingToggle.newStatus ? 'Activate' : 'Deactivate'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

function DetailRow({ label, value, roleColor, status, showFull }) {
  const textColor = roleColor || (status ? '#34d399' : 'var(--text-primary)');
  const isIdRow = showFull || (typeof value === 'string' && value.length > 20 && value.includes('-'));

  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '0.55rem 0', borderBottom: '1px solid rgba(255,255,255,0.04)', gap: '1rem' }}>
      <span style={{ color: 'var(--text-secondary)', fontWeight: 500, fontSize: '0.875rem', minWidth: isIdRow ? '50px' : '80px', flexShrink: 0 }}>{label}</span>
      <span style={{
        color: textColor,
        fontWeight: 600,
        fontSize: '0.875rem',
        textAlign: 'left',
        fontFamily: isIdRow ? 'var(--font-mono)' : 'inherit',
        letterSpacing: isIdRow ? '0.02em' : 0,
        whiteSpace: isIdRow ? 'pre' : 'normal',
        overflow: 'visible',
        textOverflow: 'clip',
        wordBreak: isIdRow ? 'normal' : 'normal',
        width: isIdRow ? '100%' : 'auto',
        flex: isIdRow ? '1' : 'none',
        lineHeight: 1.5
      }}>
        {value}
      </span>
    </div>
  );
}

const cellStyle = {
  padding: '0.55rem 1rem 0.7rem',
  textAlign: 'left',
  fontSize: '0.9rem',
  borderBottom: '1px solid rgba(255,255,255,0.12)',
};

export default AdminUsersView;
