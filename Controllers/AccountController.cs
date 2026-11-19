using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using TimeTrackingSystem.Services;

namespace TimeTrackingSystem.Controllers
{
    public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailService emailService) : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IEmailService _emailService = emailService;
        
        /*
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
        }
        */


        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            //return View();
            return View("~/Views/Account/Login.cshtml"); //TODO: Temporary fix
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {


            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail");
                return View("~/Views/Account/Login.cshtml", model); //TODO: Temporary fix
            }
            var currentUser = await _userManager.FindByEmailAsync(model.Email);
            if (currentUser == null || currentUser.IsActive == false)
            {
                ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail");
                return View("~/Views/Account/Login.cshtml", model);
            }
            else
            {
                var result = await _signInManager.PasswordSignInAsync(currentUser, model.Password, false, true);
                
                if (result.Succeeded)
                {
                    var role = await _userManager.GetRolesAsync(currentUser);
                    var roleValue = role.ElementAt(0);
                    switch(roleValue) //TODO: Add a home website for every role and finish this
                    {
                        case "Administrator":
                            {
                                return RedirectToAction("Index", "Admin");
                            }
                        case "Kierownik":
                            {
                                return RedirectToAction("Index", "Admin");
                            }
                        case "Pracownik":
                            {
                                return RedirectToAction("Index", "Admin");
                            }
                    }
                }
                else if(result.IsLockedOut)
                {
                    ModelState.AddModelError("Login", "Za dużo prób logowania, konto zostało zablokowane na 5 minut");
                    return View("~/Views/Account/Login.cshtml", model);

                }
                ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail");
                return View("~/Views/Account/Login.cshtml", model);

            }

        }

        [HttpGet, Authorize]

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);

            }
            else
            {
                var selectedUser = await _userManager.GetUserAsync(User);
                if (selectedUser != null)
                {
                    var result = await _userManager.ChangePasswordAsync(selectedUser, model.CurrentPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        TempData["Success"] = "Hasło zmienione pomyślnie!";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);

                        }
                        return View(model);
                    }

                }
                else
                {
                    return View(model);
                }
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Wylogowano pomyślnie";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && user.IsActive)
            {
                var myToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { token = myToken, email = user.Email }, Request.Scheme);
                var emailBody = $@"
                                <h2>Reset hasła</h2>
                                <p>Cześć {WebUtility.HtmlEncode(user.FirstName)},</p>
                                <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta w systemie ewidencji czasu pracy.</p>
                                <p><a href=""{resetLink}"">Kliknij tutaj, aby ustawić nowe hasło</a></p>
                                <p>Link jest jednorazowy i ważny przez 1 godzinę.</p>
                                <p>Jeśli to nie Ty prosiłeś o reset, zignoruj tę wiadomość.</p>";


                await _emailService.SendEmailAsync(user.Email!, "Reset hasła", emailBody);
                TempData["Success"] = "Sprawdź swoją skrzynke email";
                return RedirectToAction("Login", "Account");


            }
            else
            {
                TempData["Success"] = "Sprawdź swoją skrzynke email";
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            if (model.Token == null || model.Email == null)
            {
                return BadRequest();
            }
            return View(model);
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (model.Token == null || model.Email == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.FindByEmailAsync(model.Email);

            if (currentUser != null)
            {
                var result = await _userManager.ResetPasswordAsync(currentUser, model.Token, model.NewPassword);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Hasło zmienione pomyślnie";
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }



        }
    }
}
