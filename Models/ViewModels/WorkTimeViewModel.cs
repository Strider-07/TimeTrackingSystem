using System.ComponentModel.DataAnnotations;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class WorkTimeViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; }
        public TimeSpan WorkTime { get; set; } // In current month

        public WorkTimeViewModel(ApplicationUser user, TimeSpan workTime)
        {
            User = user;
            UserId = user.Id;
            WorkTime = workTime;
        }
    }
}
