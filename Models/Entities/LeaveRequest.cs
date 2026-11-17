namespace TimeTrackingSystem.Models.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string Status { get; set; } = "Oczekujący";
        public string LeaveType { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;

        public string? ReviewerId { get; set; }
        public ApplicationUser? Reviewer { get; set; }
        public string? ReviewerComment { get; set; }

        public DateTime RequestDate { get; set; }
        public DateTime? DecisionDate { get; set; }
    }
}
