export function usePickupActions() {
  const cancelPickup = async (id) => {
    const res = await fetch(`/api/pickups/${id}/cancel`, { method: "POST" });
    if (!res.ok) {
      const err = await res.json();
      alert(err.message);   // shows your backend's error message
      return;
    }
    alert("Pickup cancelled!");
  };

  const reschedulePickup = async (id, newDate) => {
    const res = await fetch(`/api/pickups/${id}/reschedule`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ newDate }),
    });
    if (!res.ok) {
      const err = await res.json();
      alert(err.message);
      return;
    }
    alert("Pickup rescheduled!");
  };

  return { cancelPickup, reschedulePickup };
}