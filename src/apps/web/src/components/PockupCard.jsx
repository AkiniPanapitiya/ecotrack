function PickupCard({ pickup, onCancel, onReschedule }) {
  const canModify = pickup.status !== "Collected";

  return (
    <div className="pickup-card">
      <p>Status: {pickup.status}</p>
      <p>Date: {pickup.scheduledDate}</p>

      {canModify && (
        <div className="pickup-actions">
          <button onClick={() => onCancel(pickup.id)}>Cancel</button>
          <button onClick={() => onReschedule(pickup.id)}>Reschedule</button>
        </div>
      )}
    </div>
  );
}

export default PickupCard;