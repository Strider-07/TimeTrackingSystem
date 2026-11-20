using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class HomeUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
    }
}
