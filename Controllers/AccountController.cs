using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            //return View();
            return View("~/Views/Account/Login.cshtml"); //TODO: Temporary fix
        }

        [HttpPost]
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
                    return RedirectToAction("Index", "Admin");
                }
                ModelState.AddModelError("Login", "Nie poprawne hasło lub / i mail");
                return View(model);
                
            }

        }
    }
}
