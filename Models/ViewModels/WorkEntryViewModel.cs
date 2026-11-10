namespace TimeTrackingSystem.Models.ViewModels
{
    public class WorkEntryViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public TimeSpan TodayWorkTime { get; set; }
        public TimeSpan ThisMonthWorkTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
