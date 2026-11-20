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
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public LeaveRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        public async Task<IActionResult> Manage()
        {
            var users = await _userManager.Users.Where(x => x.Position == "Pracownik").ToListAsync();
            List<LeaveRequest> allRequests = new List<LeaveRequest>();
            foreach (var user in users)
            {
                var requests = await _context.LeaveRequests.
                    Where(x => x.UserId == user.Id && x.Status == "Oczekujący").ToListAsync();
                allRequests.AddRange(requests);
            }
            return View("Manager/Manage", allRequests);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLeaveRequest()
        {
            return View();
        }

        [Authorize(Roles = "Pracownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeaveRequest(CreateLeaveRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), 
                    "Data zakończenia nie może być wcześniejsza od daty rozpoczęcia");
                return View(model);
            }
            //if (model.StartDate < [Dzisiaj])
            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                User = user,
                Status = "Oczekujący",
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason != null ? model.Reason : "",
                RequestDate = DateTime.UtcNow
            };
            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pomyślnie dodano wniosek";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Pracownik")]
        public async Task<IActionResult> ShowLeaveRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var requests = await _context.LeaveRequests.Where(x => x.UserId == user.Id).ToListAsync();
            return View(requests);
        }

        [Authorize(Roles = "Kierownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRequestStatus(int id, string status, string? reason)
        {
            var request = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id);
            if (request == null)
            {
                return NotFound();
            }
            request.Status = status;
            request.Reason = reason != null ? reason : "Brak";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
