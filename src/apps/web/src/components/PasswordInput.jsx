import React, { useState } from 'react';
import { Eye, EyeOff, Lock } from 'lucide-react';

export const PasswordInput = ({ id, name, value, onChange, onBlur, placeholder = '••••••••', autoComplete, hasError }) => {
  const [visible, setVisible] = useState(false);

  return (
    <div className="input-wrapper">
      <Lock size={18} className="input-icon" />
      <input
        id={id}
        type={visible ? 'text' : 'password'}
        name={name}
        className={`form-input has-icon has-toggle ${hasError ? 'input-error' : ''}`}
        placeholder={placeholder}
        value={value}
        onChange={onChange}
        onBlur={onBlur}
        autoComplete={autoComplete}
        aria-invalid={hasError ? 'true' : 'false'}
      />
      <button
        type="button"
        className="password-toggle"
        onClick={() => setVisible(v => !v)}
        aria-label={visible ? 'Hide password' : 'Show password'}
        aria-pressed={visible}
      >
        {visible ? <EyeOff size={18} /> : <Eye size={18} />}
      </button>
    </div>
  );
};