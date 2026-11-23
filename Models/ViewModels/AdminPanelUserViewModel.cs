using Microsoft.AspNetCore.Identity;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class AdminPanelUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
