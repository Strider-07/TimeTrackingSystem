using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions) : base(contextOptions)
        {

        }

        // This helps with creating propper database tabel
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<LeaveRequest>()
                .HasOne(lr => lr.User)
                .WithMany()
                .HasForeignKey(lr => lr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Reviewer)
                .WithMany()
                .HasForeignKey(lr => lr.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<WorkEntry> WorkEntries { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
    }
}
