using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    [Authorize(Roles = "Administrator,Kierownik")]
    public class DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var current = await userManager.GetUserAsync(User);
            if (current == null) return NotFound();

            var team = (await userManager.GetUsersInRoleAsync("Pracownik")).ToList();
            if (User.IsInRole("Administrator"))
                team.AddRange(await userManager.GetUsersInRoleAsync("Kierownik"));

            var users = team
                .Where(u => u.IsActive && u.Id != current.Id)
                .DistinctBy(u => u.Id)
                .ToList();
            var ids = users.Select(u => u.Id).ToList();

            var today = DateOnly.FromDateTime(DateTime.Now);
            var monthStart = new DateOnly(today.Year, today.Month, 1);

            var entries = await context.TimeEntries
                .Where(e => ids.Contains(e.UserId) && e.EntryDate >= monthStart)
                .ToListAsync();

            var workingNow = await context.TimeEntries
                .Where(e => ids.Contains(e.UserId) && e.EndTime == null)
                .Select(e => e.UserId).Distinct().CountAsync();

            var leaves = await context.LeaveRequests
                .Where(r => ids.Contains(r.UserId))
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                EmployeesCount = users.Count,
                WorkingNowCount = workingNow,
                PendingRequestsCount = leaves.Count(r => r.Status == "Oczekujący"),
                OnLeaveTodayCount = leaves
                    .Where(r => r.Status == "Zaakceptowany" && r.StartDate <= today && r.EndDate >= today)
                    .Select(r => r.UserId).Distinct().Count(),
                MonthWorkTime = entries.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime()),
                ApprovedCount = leaves.Count(r => r.Status == "Zaakceptowany"),
                PendingCount = leaves.Count(r => r.Status == "Oczekujący"),
                RejectedCount = leaves.Count(r => r.Status == "Odrzucony")
            };

            for (int d = 1; d <= today.Day; d++)
            {
                var day = new DateOnly(today.Year, today.Month, d);
                var hours = entries.Where(e => e.EntryDate == day)
                                   .Sum(e => e.WorkTime().TotalHours);
                vm.DayLabels.Add(d.ToString());
                vm.DayHours.Add(Math.Round(hours, 2));
            }

            var top = entries.GroupBy(e => e.UserId)
                .Select(g => new
                {
                    User = users.First(u => u.Id == g.Key),
                    Hours = g.Sum(e => e.WorkTime().TotalHours)
                })
                .OrderByDescending(x => x.Hours)
                .Take(5);

            foreach (var t in top)
            {
                vm.TopNames.Add($"{t.User.FirstName} {t.User.LastName}");
                vm.TopHours.Add(Math.Round(t.Hours, 2));
            }

            return View(vm);
        }
    }
}