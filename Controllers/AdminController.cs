using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using TimeTrackingSystem.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TimeTrackingSystem.Controllers
{
    //[Authorize(Roles = "Administrator")] // For testing this isn't necessary
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }


        // ---------------------- Users Management ----------------------
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View("Users", users);
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

            if (!await _roleManager.RoleExistsAsync(model.Position))
            {
                ModelState.AddModelError(nameof(model.Position), "Wybrane stanowisko nie istnieje w systemie.");
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
            var roleResult = await _userManager.AddToRoleAsync(user, model.Position);

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
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Position = model.Position;
            user.Department = model.Department;
            user.IsActive = model.IsActive;
            var result = await _userManager.UpdateAsync(user);
            // NOTE: beacuse it's copied from above, idk if this is 100% correct way for this.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.DeleteAsync(user);
            // NOTE: beacuse it's copied from above, idk if this is 100% correct way for this.
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);


                return RedirectToAction("Users");
            }
            return RedirectToAction("Users");
        }
        // ---------------------- Users Management ----------------------

        // -------------------------- Reports --------------------------
        public async Task<IActionResult> Reports()
        {
            var users = await _userManager.Users.ToListAsync();

            return View("Reports/Reports", users);
        }

        public async Task<IActionResult> WorkTimeReport()
        {
            var users = await _userManager.Users.ToListAsync(); // NOTE: maybe check the roles (using "Where")
            var allEntries = await _context.WorkEntries.ToListAsync();
            var entriesByUser = allEntries.
                GroupBy(x => x.UserId).
                ToDictionary(g => g.Key, g => g.Aggregate(TimeSpan.Zero, (sum, entry) => sum + entry.WorkTime()));
            var workTimeViewModels = users.Select(user =>
            {
                entriesByUser.TryGetValue(user.Id, out var totalWorkTime);
                return new WorkTimeViewModel(user, totalWorkTime);
            }).ToList();
            return View("Reports/WorkTimeReport", workTimeViewModels);
        }

        public async Task<IActionResult> LeaveRequestReport()
        {
            var requests = await _context.WorkEntries.ToListAsync();
            return View("Reports/LeaveRequestReport", requests);
        }
    }
}
