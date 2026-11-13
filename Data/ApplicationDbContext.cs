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

        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<WorkEntry> WorkEntries { get; set; }
    }
}
