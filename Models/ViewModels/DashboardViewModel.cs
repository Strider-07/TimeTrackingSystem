namespace TimeTrackingSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string Month { get; set; } = string.Empty;

        
        public double TotalHours { get; set; }
        public int EmployeesCount { get; set; }
        public int WorkingNow { get; set; }
        public int PendingLeaves { get; set; }


        public List<string> EmployeeLabels { get; set; } = new();
        public List<double> EmployeeHours { get; set; } = new();

        public List<string> DayLabels { get; set; } = new();
        public List<double> DayHours { get; set; } = new();

        public List<string> LeaveLabels { get; set; } = new();
        public List<int> LeaveCounts { get; set; } = new();
    }
}
