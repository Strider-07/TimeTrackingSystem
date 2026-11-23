using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    [Authorize]
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
                return NotFound();
            }

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var todayWorkEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.StartTime >= today && x.StartTime < today.AddDays(1));

            var monthEntries = await _context.WorkEntries
                .Where(x => x.UserId == user.Id && x.StartTime >= monthStart)
                .ToListAsync();

            var viewModel = new WorkEntryViewModel
            {
                FirstName = user.FirstName,
                TodayWorkTime = todayWorkEntry?.WorkTime() ?? TimeSpan.Zero,
                ThisMonthWorkTime = monthEntries.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime())
            };

            if (todayWorkEntry != null)
            {
                ViewBag.TodayWorkEntry = todayWorkEntry;
                viewModel.StartTime = todayWorkEntry.StartTime;
                viewModel.EndTime = todayWorkEntry.EndTime;
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
            var activeEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.EndTime == null);
            if (activeEntry != null)
            {
                TempData["Error"] = "Masz już rozpoczętą pracę";
                return RedirectToAction("Index"); 
            }
            var todayWorkEntry = await _context.WorkEntries.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.StartTime.Date == DateTime.UtcNow.Date);
            if (todayWorkEntry != null)
            {
                TempData["Error"] = "Dzisiaj nie możesz już tworzyć wpisów";
                return RedirectToAction("Index");
            }
            var workEntry = new WorkEntry
            {
                UserId = user.Id,
                StartTime = DateTime.UtcNow,
            };
            _context.WorkEntries.Add(workEntry);
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
            var activeEntry = await _context.WorkEntries.FirstOrDefaultAsync(
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

        public async Task<IActionResult> EntryHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var userEntries = await _context.WorkEntries.Where(x => x.UserId == user.Id).ToListAsync();

            return View("EntryHistory", userEntries);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        public async Task<IActionResult> ShowEntries(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }
            var entries = await _context.WorkEntries.Where(x => x.UserId == id).ToListAsync();
            var userEntriesViewModel = new UserEntriesViewModel(user, entries);
            return View("Manager/ShowEntries", userEntriesViewModel);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        [HttpGet]
        public async Task<IActionResult> EditEntry(int id)
        {
            var entry = await _context.WorkEntries.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
            if (entry == null)
            {
                return NotFound();
            }
            var localStart = entry.StartTime.ToLocalTime();
            var localEnd = entry.EndTime?.ToLocalTime();
            var model = new EditWorkEntryViewModel
            {
                Id = entry.Id,
                StartDate = DateOnly.FromDateTime(localStart),
                StartHour = TimeOnly.FromDateTime(localStart),
                EndDate = localEnd.HasValue ? DateOnly.FromDateTime(localEnd.Value) : null,
                EndHour = localEnd.HasValue ? TimeOnly.FromDateTime(localEnd.Value) : null
            };
            ViewBag.EmployeeName = $"{entry.User.FirstName} {entry.User.LastName}";
            ViewBag.UserId = entry.UserId;
            return View("Manager/EditEntry", model);
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEntry(EditWorkEntryViewModel model)
        {
            var entry = await _context.WorkEntries
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (entry == null)
            {
                return NotFound();
            }

            // Sprawdzenie rozpoczęcia pracy
            if (!model.StartDate.HasValue || !model.StartHour.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Data i godzina rozpoczęcia są wymagane.");
            }

            // Sprawdzenie zakończenia pracy
            if (model.EndDate.HasValue != model.EndHour.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Podaj zarówno datę, jak i godzinę zakończenia albo pozostaw oba pola puste.");
            }

            DateTime? startTime = null;
            DateTime? endTime = null;

            // Łączenie daty i godziny rozpoczęcia
            if (model.StartDate.HasValue && model.StartHour.HasValue)
            {
                startTime = model.StartDate.Value.ToDateTime(model.StartHour.Value);
            }

            // Łączenie daty i godziny zakończenia
            if (model.EndDate.HasValue && model.EndHour.HasValue)
            {
                endTime = model.EndDate.Value.ToDateTime(model.EndHour.Value);
            }

            // Sprawdzenie poprawności zakresu czasu
            if (startTime.HasValue && endTime.HasValue)
            {
                if (endTime.Value <= startTime.Value)
                {
                    ModelState.AddModelError(   
                        string.Empty,
                        "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                }
            }

            // Jeżeli występują błędy, ponownie wyświetlamy formularz
            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(entry.UserId);

                ViewBag.EmployeeName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : "Nieznany użytkownik";

                ViewBag.UserId = entry.UserId;

                return View("Manager/EditEntry", model);
            }

            // Konwersja czasu lokalnego na UTC
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

            entry.Description = model.Reason;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Zapisano zmiany wpisu";

            return RedirectToAction("ShowEntries", new { id = entry.UserId });
        }


    }
}
