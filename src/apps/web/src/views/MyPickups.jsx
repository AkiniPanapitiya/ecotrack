import PickupCard from "../components/PickupCard";
import { usePickupActions } from "../hooks/usePickupActions";

function MyPickups({ pickups }) {
  const { cancelPickup, reschedulePickup } = usePickupActions();

  return pickups.map(p => (
    <PickupCard 
      key={p.id} 
      pickup={p} 
      onCancel={cancelPickup} 
      onReschedule={reschedulePickup} 
    />
  ));
}