namespace LogisticsService.Models
{
    public class Pickup
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int RecyclerId { get; set; }
        public int UserId { get; set; }
    }
}