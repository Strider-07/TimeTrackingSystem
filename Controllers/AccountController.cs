using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public AccountController(SignInManager<ApplicationUser> signInManager,UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
    }
}
