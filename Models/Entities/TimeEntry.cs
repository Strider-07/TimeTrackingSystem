namespace TimeTrackingSystem.Models.Entities
{
    public class TimeEntry
    {

        public int Id { get; set; }
        public string UserId { get; set; }
        public DateOnly EntryDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Note {  get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
