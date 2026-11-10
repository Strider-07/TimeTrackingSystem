namespace TimeTrackingSystem.Models.Entities
{
    public class WorkEntry
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public TimeSpan WorkTime()
        {
            return (EndTime ?? DateTime.UtcNow) - StartTime;
        }
    }
}
