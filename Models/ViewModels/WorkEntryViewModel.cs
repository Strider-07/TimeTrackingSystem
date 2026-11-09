namespace TimeTrackingSystem.Models.ViewModels
{
    public class WorkEntryViewModel
    {
        public TimeSpan TodayWorkTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
