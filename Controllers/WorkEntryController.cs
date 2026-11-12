using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    //[Authorize(Roles = "Pracownik, Kierownik, Administrator")]
    public class WorkEntryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public WorkEntryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            if (await _userManager.IsInRoleAsync(user, "Kierownik"))
            {
                var employees = await _userManager.Users.Where(x => x.Position == "Pracownik").ToListAsync();
                return RedirectToAction("Manager/Index", employees);
            }
            var today = DateTime.UtcNow.Date;
            var todayWorkEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.StartTime.Date == today);
            var viewModel = new WorkEntryViewModel();
            viewModel.FirstName = user.FirstName;
            if (todayWorkEntry == null)
            {
                viewModel.TodayWorkTime = TimeSpan.Zero;
                return View(viewModel);
            }
            viewModel.TodayWorkTime = todayWorkEntry.WorkTime();
            var allEntries = await _context.WorkEntries.Where(
                x => x.UserId == user.Id && x.StartTime >= new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                .ToListAsync();
            var thisMonthWorkTime = TimeSpan.Zero;
            foreach (var entry in allEntries)
            {
                thisMonthWorkTime += entry.WorkTime();
            }
            viewModel.ThisMonthWorkTime = thisMonthWorkTime;
            viewModel.StartTime = todayWorkEntry.StartTime;
            viewModel.EndTime = todayWorkEntry.EndTime;
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartWork()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var activeEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EndTime == null);
            if (activeEntry != null)
            {
                TempData["Error"] = "Masz już rozpoczętą pracę";
                return RedirectToAction("Index"); 
            }
            var workEntry = new WorkEntry
            {
                UserId = user.Id,
                StartTime = DateTime.UtcNow,
            };
            _context.WorkEntries.Add(workEntry);
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Pomyślnie rozpoczęto pracę";

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndWork()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var activeEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EndTime == null);
            if (activeEntry == null)
            {
                TempData["Error"] = "Nie masz rozpoczętej pracy";
                return RedirectToAction("Index");
            }
            activeEntry.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Succes"] = "Pomyślnie zakończono pracę";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EntryHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var userEntries = await _context.WorkEntries.ToListAsync();

            return View("EntryHistory", userEntries);
        }


    }
}
