using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Net;
using TimeTrackingSystem.Services;
using System.Security.Claims;

namespace TimeTrackingSystem.Controllers
{
    //[Authorize(Roles = "Administrator")] // For testing this isn't necessary
    public class AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IEmailService _emailService = emailService;

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

            return View("Reports", users);
        }
    }
}
