import React, { useState } from 'react';
import { logisticsApi } from '../services/api';
import { Truck, Package, Calendar, Clock, MapPin, Phone, AlertCircle, CheckCircle2, ArrowRight, ShieldCheck } from 'lucide-react';

const CATEGORIES = [
  { id: 'Computing & Laptops', name: 'Computing & Laptops', desc: 'Desktops, Laptops, Servers, Monitors, Keyboards' },
  { id: 'Mobile & Handhelds', name: 'Mobile & Handhelds', desc: 'Smartphones, Tablets, Smartwatches, E-Readers' },
  { id: 'Home & Office Appliances', name: 'Home & Office Appliances', desc: 'Printers, Scanners, Microwaves, Audio Systems' },
  { id: 'Batteries & Power Supplies', name: 'Batteries & Power Supplies', desc: 'UPS units, Inverters, Lithium-ion Packs' },
  { id: 'Mixed / Bulk E-Waste', name: 'Mixed / Bulk E-Waste', desc: 'Assorted cables, circuit boards, industrial parts' },
];

const TIME_SLOTS = [
  'Morning (09:00 - 12:00)',
  'Afternoon (12:00 - 15:00)',
  'Evening (15:00 - 18:00)',
];

export const PickupBookingView = () => {
  const [formData, setFormData] = useState({
    category: 'Computing & Laptops',
    estimatedWeightKg: '',
    pickupAddress: '',
    contactPhone: '',
    preferredDate: '',
    timeSlot: 'Morning (09:00 - 12:00)',
    specialInstructions: '',
  });

  const [errors, setErrors] = useState({});
  const [serverError, setServerError] = useState('');
  const [successBooking, setSuccessBooking] = useState(null);
  const [loading, setLoading] = useState(false);

  const validateForm = () => {
    const newErrors = {};

    if (!formData.category) {
      newErrors.category = 'Please select an e-waste category.';
    }

    const weight = parseFloat(formData.estimatedWeightKg);
    if (isNaN(weight) || weight <= 0) {
      newErrors.estimatedWeightKg = 'Please enter a valid estimated weight in Kg.';
    } else if (weight > 10000) {
      newErrors.estimatedWeightKg = 'Maximum single pickup weight limit is 10,000 kg.';
    }

    if (!formData.pickupAddress.trim()) {
      newErrors.pickupAddress = 'Pickup address is required.';
    } else if (formData.pickupAddress.trim().length < 5) {
      newErrors.pickupAddress = 'Address must be at least 5 characters.';
    }

    if (!formData.contactPhone.trim()) {
      newErrors.contactPhone = 'Contact phone number is required.';
    }

    if (!formData.preferredDate) {
      newErrors.preferredDate = 'Please select a preferred pickup date.';
    } else {
      const selected = new Date(formData.preferredDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (selected < today) {
        newErrors.preferredDate = 'Pickup date cannot be in the past.';
      }
    }

    if (!formData.timeSlot) {
      newErrors.timeSlot = 'Please select a preferred time slot.';
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
    setSuccessBooking(null);

    const payload = {
      category: formData.category,
      estimatedWeightKg: parseFloat(formData.estimatedWeightKg),
      pickupAddress: formData.pickupAddress.trim(),
      contactPhone: formData.contactPhone.trim(),
      preferredDate: formData.preferredDate,
      timeSlot: formData.timeSlot,
      specialInstructions: formData.specialInstructions.trim() || null,
    };

    try {
      const res = await logisticsApi.createPickup(payload);
      setSuccessBooking(res.data);
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to schedule pickup request. Please try again.';
      setServerError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '800px', margin: '2rem auto', padding: '0 1rem' }}>
      <div className="glass-card">
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
            <Truck size={32} style={{ color: 'var(--primary)' }} />
          </div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            Schedule E-Waste Pickup
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>
            Book a certified eco-friendly collection slot for your electronic waste items
          </p>
        </div>

        {serverError && (
          <div className="alert alert-danger" style={{ marginBottom: '1.5rem' }}>
            <AlertCircle size={18} />
            <span>{serverError}</span>
          </div>
        )}

        {successBooking && (
          <div className="alert alert-success" style={{ marginBottom: '1.5rem' }}>
            <CheckCircle2 size={24} style={{ color: 'var(--success)', flexShrink: 0 }} />
            <div>
              <strong style={{ fontSize: '1.05rem', display: 'block', marginBottom: '0.25rem' }}>
                Pickup Scheduled Successfully!
              </strong>
              <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                Booking Reference ID: <code style={{ background: 'rgba(0,0,0,0.3)', padding: '2px 6px', borderRadius: '4px' }}>{successBooking.id}</code><br />
                Category: <strong>{successBooking.category}</strong> ({successBooking.estimatedWeightKg} kg)<br />
                Scheduled for: <strong>{new Date(successBooking.preferredDate).toLocaleDateString()}</strong> ({successBooking.timeSlot})
              </div>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* E-Waste Category Grid */}
          <div className="form-group">
            <label className="form-label" style={{ fontWeight: 700, marginBottom: '0.75rem' }}>
              Select E-Waste Category *
            </label>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '0.75rem' }}>
              {CATEGORIES.map(cat => (
                <div
                  key={cat.id}
                  onClick={() => { setFormData(prev => ({ ...prev, category: cat.id })); if (errors.category) setErrors(prev => ({ ...prev, category: '' })); }}
                  style={{
                    padding: '1rem',
                    borderRadius: 'var(--radius-md)',
                    border: formData.category === cat.id ? '2px solid var(--primary)' : '1px solid var(--border-color)',
                    background: formData.category === cat.id ? 'var(--primary-light)' : 'rgba(255,255,255,0.02)',
                    cursor: 'pointer',
                    transition: 'all var(--transition-fast)'
                  }}
                >
                  <div style={{ fontWeight: 700, fontSize: '0.95rem', color: formData.category === cat.id ? 'var(--primary)' : 'var(--text-primary)', marginBottom: '0.25rem' }}>
                    {cat.name}
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', lineHeight: 1.3 }}>
                    {cat.desc}
                  </div>
                </div>
              ))}
            </div>
            {errors.category && <div className="form-error"><AlertCircle size={14} />{errors.category}</div>}
          </div>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Estimated Weight (Kg) *</label>
              <div style={{ position: 'relative' }}>
                <input
                  type="number"
                  step="0.1"
                  name="estimatedWeightKg"
                  className="form-input"
                  placeholder="e.g. 15.5"
                  value={formData.estimatedWeightKg}
                  onChange={handleChange}
                />
                <span style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>
                  Kg
                </span>
              </div>
              {errors.estimatedWeightKg && <div className="form-error"><AlertCircle size={14} />{errors.estimatedWeightKg}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Contact Phone *</label>
              <div style={{ position: 'relative' }}>
                <Phone size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                <input
                  type="tel"
                  name="contactPhone"
                  className="form-input"
                  style={{ paddingLeft: '38px' }}
                  placeholder="+94 77 123 4567"
                  value={formData.contactPhone}
                  onChange={handleChange}
                />
              </div>
              {errors.contactPhone && <div className="form-error"><AlertCircle size={14} />{errors.contactPhone}</div>}
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Pickup Address *</label>
            <div style={{ position: 'relative' }}>
              <MapPin size={16} style={{ position: 'absolute', left: '12px', top: '14px', color: 'var(--text-secondary)' }} />
              <textarea
                name="pickupAddress"
                className="form-input"
                style={{ paddingLeft: '38px', minHeight: '75px', resize: 'vertical' }}
                placeholder="No. 45, Green Way, Industrial Park, Colombo"
                value={formData.pickupAddress}
                onChange={handleChange}
              />
            </div>
            {errors.pickupAddress && <div className="form-error"><AlertCircle size={14} />{errors.pickupAddress}</div>}
          </div>

          <div className="grid-2">
            <div className="form-group">
              <label className="form-label">Preferred Date *</label>
              <div style={{ position: 'relative' }}>
                <Calendar size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                <input
                  type="date"
                  name="preferredDate"
                  className="form-input"
                  style={{ paddingLeft: '38px' }}
                  value={formData.preferredDate}
                  onChange={handleChange}
                />
              </div>
              {errors.preferredDate && <div className="form-error"><AlertCircle size={14} />{errors.preferredDate}</div>}
            </div>

            <div className="form-group">
              <label className="form-label">Time Slot *</label>
              <div style={{ position: 'relative' }}>
                <Clock size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                <select
                  name="timeSlot"
                  className="form-input"
                  style={{ paddingLeft: '38px' }}
                  value={formData.timeSlot}
                  onChange={handleChange}
                >
                  {TIME_SLOTS.map(slot => (
                    <option key={slot} value={slot}>{slot}</option>
                  ))}
                </select>
              </div>
              {errors.timeSlot && <div className="form-error"><AlertCircle size={14} />{errors.timeSlot}</div>}
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Special Instructions (Optional)</label>
            <textarea
              name="specialInstructions"
              className="form-input"
              style={{ minHeight: '60px', resize: 'vertical' }}
              placeholder="e.g. Items are packed in 2 boxes by the security gate."
              value={formData.specialInstructions}
              onChange={handleChange}
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', padding: '0.9rem', fontSize: '1rem', marginTop: '0.5rem' }}
            disabled={loading}
          >
            {loading ? 'Booking Collection...' : 'Confirm Pickup Booking'}
            <ArrowRight size={18} />
          </button>
        </form>
      </div>
    </div>
  );
};
