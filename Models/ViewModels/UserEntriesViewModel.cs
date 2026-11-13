using System.ComponentModel.DataAnnotations;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class UserEntriesViewModel
    {
        [Required]
        public ApplicationUser User { get; set; }
        [Required]
        public List<WorkEntry> WorkEntries { get; set; }

        public UserEntriesViewModel(ApplicationUser user,  List<WorkEntry> workEntries)
        {
            User = user;
            WorkEntries = workEntries;
        }
    }
}
