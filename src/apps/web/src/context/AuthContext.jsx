import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi } from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    const savedUser = localStorage.getItem('ecotrack_user');
    return savedUser ? JSON.parse(savedUser) : null;
  });
  const [token, setToken] = useState(() => localStorage.getItem('ecotrack_token'));
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (token) {
      localStorage.setItem('ecotrack_token', token);
    } else {
      localStorage.removeItem('ecotrack_token');
    }
  }, [token]);

  useEffect(() => {
    if (user) {
      localStorage.setItem('ecotrack_user', JSON.stringify(user));
    } else {
      localStorage.removeItem('ecotrack_user');
    }
  }, [user]);

  const login = async (email, password) => {
    setLoading(true);
    try {
      const response = await authApi.login({ email, password });
      const { token: jwtToken, ...userData } = response.data;
      setToken(jwtToken);
      setUser(userData);
      return { success: true, data: response.data };
    } catch (error) {
      const message = error.response?.data?.message || 'Login failed. Please try again.';
      return { success: false, error: message };
    } finally {
      setLoading(false);
    }
  };

  const register = async (formData) => {
    setLoading(true);
    try {
      const response = await authApi.register(formData);
      return { success: true, data: response.data };
    } catch (error) {
      const message = error.response?.data?.message || 'Registration failed.';
      const errors = error.response?.data?.errors || null;
      return { success: false, error: message, validationErrors: errors };
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('ecotrack_token');
    localStorage.removeItem('ecotrack_user');
  };

  const refreshProfile = async () => {
    if (!token) return null;
    try {
      const response = await authApi.getProfile();
      const profileData = response.data;
      setUser((prev) => ({ ...prev, ...profileData }));
      return profileData;
    } catch (error) {
      console.error('Failed to fetch profile:', error);
      return null;
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        loading,
        login,
        register,
        logout,
        refreshProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
