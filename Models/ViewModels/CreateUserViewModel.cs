using System.ComponentModel.DataAnnotations;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string LastName { get; set; } = "";
        [Required]
        public string Position { get; set; } = "";
        [Required]
        public string Department { get; set; } = "";
        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
    }
}
