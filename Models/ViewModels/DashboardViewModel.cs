namespace TimeTrackingSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int EmployeesCount { get; set; }
        public int WorkingNowCount { get; set; }
        public int OnLeaveTodayCount { get; set; }
        public int PendingRequestsCount { get; set; }
        public TimeSpan MonthWorkTime { get; set; }

        public List<string> DayLabels { get; set; } = new();
        public List<double> DayHours { get; set; } = new();

        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }

        public List<string> TopNames { get; set; } = new();
        public List<double> TopHours { get; set; } = new();
    }
}