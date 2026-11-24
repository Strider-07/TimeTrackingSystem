namespace TimeTrackingSystem.Models.Entities
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public DateOnly EntryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Note { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser? ModifiedBy { get; set; }

        public int TotalHours()
        {
            return WorkTime().Hours;
        }

        public TimeSpan WorkTime()
        {
            return (EndTime ?? DateTime.UtcNow) - StartTime;
        }
    }
}
