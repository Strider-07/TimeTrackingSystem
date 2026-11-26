using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    [Authorize(Roles = "Administrator,Kierownik,Pracownik")]
    public class TimeEntryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public TimeEntryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var todayTimeEntry = await _context.TimeEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.StartTime >= today && x.StartTime < today.AddDays(1));
            var monthEntries = await _context.TimeEntries
                .Where(x => x.UserId == user.Id && x.StartTime >= monthStart)
                .ToListAsync();
            var viewModel = new TimeEntryViewModel
            {
                FirstName = user.FirstName,
                TodayWorkTime = todayTimeEntry?.WorkTime() ?? TimeSpan.Zero,
                ThisMonthWorkTime = monthEntries.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime())
            };

            if (todayTimeEntry != null)
            {
                ViewBag.TodayTimeEntry = todayTimeEntry;
                viewModel.StartTime = todayTimeEntry.StartTime;
                viewModel.EndTime = todayTimeEntry.EndTime;
            }
            return View(viewModel);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        public async Task<IActionResult> Users()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }
            var allowedUsers = new List<ApplicationUser>();
            if (await _userManager.IsInRoleAsync(currentUser, "Administrator"))
            {
                var managers = await _userManager.GetUsersInRoleAsync("Kierownik");
                var employees = await _userManager.GetUsersInRoleAsync("Pracownik");
                allowedUsers.AddRange(managers);
                allowedUsers.AddRange(employees);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Kierownik"))
            {
                var employees = await _userManager.GetUsersInRoleAsync("Pracownik");
                allowedUsers.AddRange(employees);
            }

            var users = allowedUsers
                .Where(x => x.Id != currentUser.Id)
                .DistinctBy(x => x.Id)
                .ToList();
            var adminPanelUsers = new List<AdminPanelUserViewModel>(); // NOTE maybe change the name of ViewModel beacuse it's universal
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Any() || roles.Count > 1)
                {
                    continue;
                }
                var role = roles[0];
                var adminPanelUser = new AdminPanelUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = role,
                    Position = user.Position,
                    Department = user.Department,
                    IsActive = user.IsActive
                };
                adminPanelUsers.Add(adminPanelUser);
            }
            return View("Manager/Users", adminPanelUsers);
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
            var activeEntry = await _context.TimeEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EndTime == null);
            if (activeEntry != null)
            {
                TempData["Error"] = "Masz już rozpoczętą pracę";
                return RedirectToAction("Index"); 
            }
            var today = DateOnly.FromDateTime(DateTime.Now);
            var todayTimeEntry = await _context.TimeEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EntryDate == today);
            if (todayTimeEntry != null)
            {

                TempData["Error"] = "Dzisiaj nie możesz już tworzyć wpisów";

                return RedirectToAction("Index");
            }
            var now = DateTime.UtcNow;
            var localNow = now.ToLocalTime();
            var TimeEntry = new TimeEntry
            {
                UserId = user.Id,
                StartTime = now,
                CreatedAt = now,
                EntryDate = DateOnly.FromDateTime(localNow)
            };
            _context.TimeEntries.Add(TimeEntry);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pomyślnie rozpoczęto pracę";
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
            var activeEntry = await _context.TimeEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EndTime == null);
            if (activeEntry == null)
            {
                TempData["Error"] = "Nie masz rozpoczętej pracy";
                return RedirectToAction("Index");
            }
            activeEntry.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pomyślnie zakończono pracę";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EntryHistory(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            const int pageSize = 10;
            var query = _context.TimeEntries
                .Include(x => x.ModifiedBy).Where(x => x.UserId == user.Id)
                .OrderByDescending(e => e.EntryDate);
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);
            var entries = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var model = new PaginationViewModel<TimeEntry>
            {
                Items = entries,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };
            return View("EntryHistory", model);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        public async Task<IActionResult> ShowEntries(string id, int page = 1)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }
            const int pageSize = 3;
            var query = _context.TimeEntries
                .Include(x => x.ModifiedBy).Where(x => x.UserId == id)
                .OrderByDescending(e => e.EntryDate);
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);
            var entries = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var paginationModel = new PaginationViewModel<TimeEntry>
            {
                Items = entries,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };
            var userEntriesViewModel = new UserEntriesViewModel(user, paginationModel);
            return View("Manager/ShowEntries", userEntriesViewModel);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        [HttpGet]
        public async Task<IActionResult> EditEntry(int id)
        {
            var entry = await _context.TimeEntries.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
            if (entry == null)
            {
                return NotFound();
            }
            var localStart = entry.StartTime.ToLocalTime();
            var localEnd = entry.EndTime?.ToLocalTime();
            if (localEnd < localStart)
            {
                ModelState.AddModelError(nameof(localEnd),
                    "Data zakończenia nie może być wcześniejsza od daty rozpoczęcia");
                return View(id);
            }
            var model = new EditTimeEntryViewModel
            {
                Id = entry.Id,
                StartDate = DateOnly.FromDateTime(localStart),
                StartHour = TimeOnly.FromDateTime(localStart),
                EndDate = localEnd.HasValue ? DateOnly.FromDateTime(localEnd.Value) : null,
                EndHour = localEnd.HasValue ? TimeOnly.FromDateTime(localEnd.Value) : null,
                Note = entry.Note
            };
            ViewBag.EmployeeName = $"{entry.User.FirstName} {entry.User.LastName}";
            ViewBag.UserId = entry.UserId;
            return View("Manager/EditEntry", model);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEntry(EditTimeEntryViewModel model)
        {
            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(x => x.Id == model.Id);
            if (entry == null)
            {
                return NotFound();
            }
            if (!model.StartDate.HasValue || !model.StartHour.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Data i godzina rozpoczęcia są wymagane.");
            }
            if (model.EndDate.HasValue != model.EndHour.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Podaj zarówno datę, jak i godzinę zakończenia albo pozostaw oba pola puste.");
            }
            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(entry.UserId);

                ViewBag.EmployeeName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : "Nieznany użytkownik";

                ViewBag.UserId = entry.UserId;

                return View("Manager/EditEntry", model);
            }
            DateTime? startTime = null;
            DateTime? endTime = null;
            if (model.StartDate.HasValue && model.StartHour.HasValue)
            {
                startTime = model.StartDate.Value.ToDateTime(model.StartHour.Value);
            }
            if (model.EndDate.HasValue && model.EndHour.HasValue)
            {
                endTime = model.EndDate.Value.ToDateTime(model.EndHour.Value);
            }
            if (startTime.HasValue && endTime.HasValue)
            {
                if (endTime.Value <= startTime.Value)
                {
                    ModelState.AddModelError(   
                        string.Empty,
                        "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                }
            }
            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(entry.UserId);

                ViewBag.EmployeeName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : "Nieznany użytkownik";

                ViewBag.UserId = entry.UserId;

                return View("Manager/EditEntry", model);
            }
            entry.StartTime = DateTime.SpecifyKind(
                startTime!.Value,
                DateTimeKind.Local
            ).ToUniversalTime();
            entry.EndTime = endTime.HasValue
                ? DateTime.SpecifyKind(
                    endTime.Value,
                    DateTimeKind.Local
                ).ToUniversalTime()
                : null;
            entry.Note = !string.IsNullOrWhiteSpace(model.Note)
                ? model.Note
                : "Brak uwag";
            entry.ModifiedBy = await _userManager.GetUserAsync(User);
            entry.EntryDate = model.StartDate!.Value;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Zapisano zmiany wpisu";
            return RedirectToAction("ShowEntries", new { id = entry.UserId });
        }
    }
}
