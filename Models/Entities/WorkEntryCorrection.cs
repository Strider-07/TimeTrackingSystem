namespace TimeTrackingSystem.Models.Entities
{
    public class WorkEntryCorrection
    {
        public int Id { get; set; }
        public int WorkEntryId { get; set; }
        public WorkEntry WorkEntry { get; set; }
        public int ManagerId { get; set; }
        public ApplicationUser Manager { get; set; }
        public DateTime OldStartTime { get; set; }
        public DateTime? OldEndTime { get; set; }
        public DateTime NewStartTime { get; set; }
        public DateTime? NewEndTime { get; set; }

        public string Reason { get; set; } = string.Empty;
        public DateTime CorrectedAt { get; set; }
    }
}
