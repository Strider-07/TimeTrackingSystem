namespace TimeTrackingSystem.Models.Entities
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public DateOnly EntryDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal TotalHours { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ApplicationUser? ModifiedBy { get; set; }

        public void CalculateTotalHours()
        {
            TotalHours = EndTime.HasValue
                ? Math.Round((decimal)(EndTime.Value - StartTime).TotalHours, 2)
                : 0;
        }

        public TimeSpan WorkTime()
        {
            return (EndTime ?? DateTime.UtcNow) - StartTime;
        }
    }
}
