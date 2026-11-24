using System.ComponentModel.DataAnnotations;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class UserEntriesViewModel
    {
        [Required]
        public ApplicationUser User { get; set; }
        [Required]
        public List<TimeEntry> TimeEntries { get; set; }

        public UserEntriesViewModel(ApplicationUser user,  List<TimeEntry> timeEntries)
        {
            User = user;
            TimeEntries = timeEntries;
        }
    }
}
