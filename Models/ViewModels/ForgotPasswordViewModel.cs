using System.ComponentModel.DataAnnotations;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required,EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
