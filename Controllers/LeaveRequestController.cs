using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;

namespace TimeTrackingSystem.Controllers
{
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

        //TODO: propably I should make a seperate view for creating request
        [HttpGet]
        public async Task<IActionResult> CreateLeaveRequest()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeaveRequest(CreateLeaveRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                User = user,
                Status = "Oczekujący",
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                RequestDate = DateTime.UtcNow
            };
            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pomyślnie dodano wniosek";
            return View();
        }

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
    }
}
