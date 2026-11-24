using System.ComponentModel.DataAnnotations;

namespace TimeTrackingSystem.Models.ViewModels
{
    public class EditTimeEntryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana.")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana.")]
        public TimeOnly? StartHour { get; set; }

        public DateOnly? EndDate { get; set; }

        public TimeOnly? EndHour { get; set; }

        [Required(ErrorMessage = "Podaj powód korekty.")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}