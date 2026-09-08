export function usePickupActions() {
  const authHeaders = () => {
    const token = localStorage.getItem('ecotrack_token');
    return token ? { Authorization: `Bearer ${token}` } : {};
  };

  const cancelPickup = async (id) => {
    try {
      const res = await fetch(`/api/pickup/${id}/cancel`, {
        method: "POST",
        headers: { ...authHeaders() },
      });

      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        return { success: false, message: err.message || `Cancel failed (${res.status})` };
      }

      return { success: true, message: "Pickup cancelled." };
    } catch (err) {
      console.error("cancelPickup network error:", err);
      return { success: false, message: "Network error — could not reach the server." };
    }
  };

  const reschedulePickup = async (id, newDate) => {
    try {
      const res = await fetch(`/api/pickup/${id}/reschedule`, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeaders() },
        body: JSON.stringify({ newDate }),
      });

      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        return { success: false, message: err.message || `Reschedule failed (${res.status})` };
      }

      return { success: true, message: "Pickup rescheduled." };
    } catch (err) {
      console.error("reschedulePickup network error:", err);
      return { success: false, message: "Network error — could not reach the server." };
    }
  };

  const getMyPickups = async (userId) => {
    const res = await fetch(`/api/pickup/user/${userId}`, {
      headers: { ...authHeaders() },
    });
    if (!res.ok) {
      throw new Error(`Failed to load pickups (${res.status})`);
    }
    return res.json();
  };

  return { cancelPickup, reschedulePickup, getMyPickups };
}