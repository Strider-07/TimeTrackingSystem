using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using TimeTrackingSystem.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace TimeTrackingSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService, IWebHostEnvironment env) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IEmailService _emailService = emailService;
        private readonly IWebHostEnvironment _env = env;

        /*
        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }
        */

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }


        // ---------------------- Users Management ----------------------
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var adminPanelUsers = new List<AdminPanelUserViewModel>();
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
            return View("Users", adminPanelUsers);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Wybrane stanowisko nie istnieje w systemie.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                Email = model.Email,
                EmailConfirmed = true,
                UserName = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Position = model.Position,
                Department = model.Department,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };


            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }
            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            var emailBody = $@"
            <h2>Witamy w systemie ewidencji czasu pracy, {WebUtility.HtmlEncode(user.FirstName)}!</h2>
            <p>Twoje konto zostało utworzone. Dane do logowania:</p>
            <ul>
                <li>Login: {WebUtility.HtmlEncode(model.Email)}</li>
                <li>Hasło: {WebUtility.HtmlEncode(model.Password)}</li>
            </ul>
            <p>Po pierwszym zalogowaniu zmień hasło.</p>";

            await _emailService.SendEmailAsync(model.Email, "Witamy w systemie ewidencji czasu pracy", emailBody);

            TempData["Success"] = "Użytkownik został dodany";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any() || roles.Count() > 1)
            {
                return NotFound(); // TODO: fix to smth better
            }
            var viewModel = new EditUserViewModel
            {
                UserId = user.Id,
                Role = roles[0],
                FirstName = user.FirstName,
                LastName = user.LastName,
                Position = user.Position,
                Department = user.Department,
                Email = user.Email != null ? user.Email : user.FirstName + "@example.com" // idk about this
            };
            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                TempData["Error"] = "Użytkownik nie ma adresu e-mail.";
                return RedirectToAction("Users");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token, email = user.Email }, Request.Scheme);

            var body = $@"
            <h2>Reset hasła</h2>
            <p>Cześć {WebUtility.HtmlEncode(user.FirstName)},</p>
            <p>Administrator zainicjował reset Twojego hasła.</p>
            <p><a href=""{resetLink}"">Kliknij tutaj, aby ustawić nowe hasło</a></p>
            <p>Link jest jednorazowy i ważny przez 1 godzinę.</p>";

            await _emailService.SendEmailAsync(user.Email!, "Reset hasła", body);
            TempData["Success"] = "Wysłano link do resetu hasła";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }
            user.Email = model.Email;
            user.UserName = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Position = model.Position;
            user.Department = model.Department;
            var result = await _userManager.UpdateAsync(user);
            // NOTE: beacuse it's copied from above, idk if this is 100% correct way for this.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.FirstOrDefault() != model.Role)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserActivation(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Users");
        }
        // ---------------------- Users Management ----------------------

        // -------------------------- Reports --------------------------
        public async Task<IActionResult> Reports()
        {
            var users = await _userManager.Users.ToListAsync();

            return View("Reports/Reports", users);
        }

        private async Task<(List<WorkTimeViewModel> Report, DateTime Month)> BuildWorkTimeReport(string? month)
        {
            var selected = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) ? parsed : DateTime.UtcNow;

            var start = new DateTime(selected.Year, selected.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var users = await _userManager.Users.ToListAsync();
            var entries = await _context.TimeEntries
                .Where(x => x.StartTime >= start && x.StartTime < end)
                .ToListAsync();

            var byUser = entries.GroupBy(x => x.UserId).ToDictionary(
                g => g.Key,
                g => g.Aggregate(TimeSpan.Zero, (t, e) => t + e.WorkTime()));

            var report = users
                .Select(u => new WorkTimeViewModel(u, byUser.TryGetValue(u.Id, out var t) ? t : TimeSpan.Zero))
                .ToList();
            return (report, start);
        }

        public async Task<IActionResult> WorkTimeReport(string? month)
        {
            var (report, selected) = await BuildWorkTimeReport(month);
            ViewBag.Month = selected.ToString("yyyy-MM");
            return View("Reports/WorkTimeReport", report);
        }

        public async Task<IActionResult> ExportWorkTimeReport(string? month)
        {
            static string Fmt(TimeSpan t) => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            var (report, selected) = await BuildWorkTimeReport(month);
            var pl = CultureInfo.GetCultureInfo("pl-PL");
            var csv = new StringBuilder();
            csv.AppendLine("Imię;Nazwisko;Stanowisko;Godziny pracy");
            foreach (var item in report)
            {
                csv.AppendLine(string.Join(";",
                    EscapeCsv(item.User.FirstName),
                    EscapeCsv(item.User.LastName),
                    EscapeCsv(item.User.Position),
                    Fmt(item.WorkTime)));
            }
            var total = report.Aggregate(TimeSpan.Zero, (t, i) => t + i.WorkTime);
            csv.AppendLine();
            csv.AppendLine($"Łącznie;;;{Fmt(total)}");
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"RaportCzasuPracy_{selected:yyyy-MM}.csv");
        }

        public async Task<IActionResult> LeaveRequestReport()
        {
            var requests = await _context.LeaveRequests.Include(x => x.User).OrderByDescending(x => x.RequestDate).ToListAsync();
            return View("Reports/LeaveRequestReport", requests);
        }

        public async Task<IActionResult> ExportLeaveRequestReport()
        {
            var requests = await _context.LeaveRequests.Include(x => x.User).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("Imię;Nazwisko;Stanowisko;Status wniosku;Rodzaj;Data rozpoczęcia;Data zakończenia;Uzasadnienie");
            foreach (var request in requests)
            {
                csv.AppendLine(string.Join(";",
                    EscapeCsv(request.User.FirstName),
                    EscapeCsv(request.User.LastName),
                    EscapeCsv(request.User.Position),
                    EscapeCsv(request.Status),
                    EscapeCsv(request.LeaveType),
                    request.StartDate.ToString("dd.MM.yyyy"),
                    request.EndDate.ToString("dd.MM.yyyy"),
                    EscapeCsv(request.Reason)));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", "RaportWnioskówUrlopowych.csv");
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public async Task<IActionResult> ExportWorkTimeReportPdf(string? month)
        {
            var (report, selected) = await BuildWorkTimeReport(month);
            var bytes = PdfReportBuilder.WorkTimeReport(report, selected, _env.WebRootPath);
            return File(bytes, "application/pdf", $"RaportCzasuPracy_{selected:yyyy-MM}.pdf");
        }

        public async Task<IActionResult> ExportLeaveRequestReportPdf()
        {
            var requests = await _context.LeaveRequests
                .Include(x => x.User)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();
            var bytes = PdfReportBuilder.LeaveRequestReport(requests, _env.WebRootPath);
            return File(bytes, "application/pdf", "RaportWnioskowUrlopowych.pdf");
        }
    }
}
