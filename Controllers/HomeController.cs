using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    public HomeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }
        var roles = await _userManager.GetRolesAsync(user);
        var role = "";
        if (!roles.Any())
        {
            role = "Brak";
        }
        else
        {
            role = roles[0];
        }
        var homeUser = new HomeUserViewModel
        {
            UserId = user.Id,
            User = user,
            Role = role
        };
        return View("Index", homeUser);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
