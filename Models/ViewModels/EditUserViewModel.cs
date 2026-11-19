using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string UserId { get; set; } = "";
        [Required]
        public string Role { get; set; } = "";
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string LastName { get; set; } = "";
        [Required]
        public string Position { get; set; } = "";
        [Required]
        public string Department { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
