using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;


        [Required,MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
        [Required, PasswordPropertyText, Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
