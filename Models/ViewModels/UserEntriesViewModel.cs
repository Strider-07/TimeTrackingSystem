using System.ComponentModel.DataAnnotations;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class UserEntriesViewModel
    {
        [Required]
        public ApplicationUser User { get; set; }
        [Required]
        public PaginationViewModel<TimeEntry> TimeEntries { get; set; }

        public UserEntriesViewModel(ApplicationUser user, PaginationViewModel<TimeEntry> timeEntries)
        {
            User = user;
            TimeEntries = timeEntries;
        }
    }
}
