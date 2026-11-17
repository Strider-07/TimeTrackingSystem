using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using TimeTrackingSystem.Services;

namespace TimeTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        public AccountController(SignInManager<ApplicationUser> signInManager,UserManager<ApplicationUser> userManager,IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
        }

        
        [HttpGet,AllowAnonymous]
        public IActionResult Login()
        {
            //return View();
            return View("~/Views/Account/Login.cshtml"); //TODO: Temporary fix
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult>Login(LoginViewModel model)
        {
            if(!ModelState.IsValid)
            {
                //ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail"); //TODO: Change the error message because the error appears after the email or password are not validated(ex. the email lack a '@' character)
                return View("~/Views/Account/Login.cshtml",model); //TODO: Temporary fix
            }
            else
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, true);
                if(result.Succeeded)
                {
                    return RedirectToAction("Index", "Admin"); //TODO: Check if its supposed to redirect there (Roles)
                }
                ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail");
                return View(model);
                
            }

        }

        [HttpGet,Authorize]
        
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]

        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);

            }
            else
            {
                var selectedUser = await _userManager.GetUserAsync(User);
                if(selectedUser != null)
                {
                    var result = await _userManager.ChangePasswordAsync(selectedUser,model.CurrentPassword,model.NewPassword);
                    if(result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        TempData["Success"] = "Hasło zmienione pomyślnie!";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach(var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty,error.Description);
                            
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

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Wylogowano pomyślnie";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet,AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost,AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email); //TODO: Check the note on the FindByEmailAsync function
            if(user != null)
            {
                var myToken = _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { token = myToken, email = user.Email }, Request.Scheme);
                var emailBody = $@"var emailBody = $@""
                                <h2>Reset hasła</h2>
                                <p>Cześć {WebUtility.HtmlEncode(user.FirstName)},</p>
                                <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta w systemie ewidencji czasu pracy.</p>
                                <p><a href=""""{{resetLink}}"""">Kliknij tutaj, aby ustawić nowe hasło</a></p>
                                <p>Link jest jednorazowy i ważny przez ograniczony czas.</p>
                                <p>Jeśli to nie Ty prosiłeś o reset, zignoruj tę wiadomość.</p>"";";

                await _emailService.SendEmailAsync(user.Email!, "Reset hasła", emailBody);
                TempData["Success"] = "Sprawdź swoją skrzynke email";
                return RedirectToAction("Login", "Account");


            }
            else
            {
                return View();
            }
        }
    }
}
