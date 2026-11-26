using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    [Authorize(Roles = "Administrator,Kierownik")]
    public class DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        private static readonly string[] LeaveStatuses = { "Oczekujący", "Zaakceptowany", "Odrzucony" };

        public async Task<IActionResult> Index(string? month)
        {
            var selected = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;

            var start = new DateTime(selected.Year, selected.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var daysInMonth = DateTime.DaysInMonth(selected.Year, selected.Month);

            
            var users = new List<ApplicationUser>();
            users.AddRange(await _userManager.GetUsersInRoleAsync("Pracownik"));
            if (User.IsInRole("Administrator"))
            {
                users.AddRange(await _userManager.GetUsersInRoleAsync("Kierownik"));
            }
            users = users.Where(u => u.IsActive).DistinctBy(u => u.Id).OrderBy(u => u.LastName).ToList();
            var ids = users.Select(u => u.Id).ToList();

            var entries = await _context.TimeEntries
                .Where(x => ids.Contains(x.UserId) && x.StartTime >= start && x.StartTime < end)
                .ToListAsync();

            var workingNow = await _context.TimeEntries
                .CountAsync(x => ids.Contains(x.UserId) && x.EndTime == null);

            var monthStart = DateOnly.FromDateTime(start);
            var monthEnd = DateOnly.FromDateTime(end.AddDays(-1));

            var leaves = await _context.LeaveRequests
                .Where(x => ids.Contains(x.UserId) && x.StartDate <= monthEnd && x.EndDate >= monthStart)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                Month = start.ToString("yyyy-MM"),
                EmployeesCount = users.Count,
                WorkingNow = workingNow,
                PendingLeaves = leaves.Count(x => x.Status == "Oczekujący")
            };

            
            var byUser = entries.GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime()));

            foreach (var u in users)
            {
                model.EmployeeLabels.Add($"{u.FirstName} {u.LastName}".Trim());
                var hours = byUser.TryGetValue(u.Id, out var t) ? t.TotalHours : 0;
                model.EmployeeHours.Add(Math.Round(hours, 2));
            }
            model.TotalHours = Math.Round(model.EmployeeHours.Sum(), 2);

            
            var byDay = entries.GroupBy(x => x.EntryDate.Day)
                .ToDictionary(g => g.Key, g => g.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime()));

            for (int day = 1; day <= daysInMonth; day++)
            {
                model.DayLabels.Add(day.ToString());
                var hours = byDay.TryGetValue(day, out var t) ? t.TotalHours : 0;
                model.DayHours.Add(Math.Round(hours, 2));
            }

            
            foreach (var status in LeaveStatuses)
            {
                model.LeaveLabels.Add(status);
                model.LeaveCounts.Add(leaves.Count(x => x.Status == status));
            }

            return View(model);
        }
    }
}