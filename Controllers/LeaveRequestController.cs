using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using TimeTrackingSystem.Models.ViewModels;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using TimeTrackingSystem.Services;


namespace TimeTrackingSystem.Controllers
{
    [Authorize]
    public class LeaveRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        private readonly IEmailService _emailService = emailService;
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [Authorize(Roles = "Administrator,Kierownik")]
        public async Task<IActionResult> Manage()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }
            var allowedUsers = new List<ApplicationUser>();
            if (await _userManager.IsInRoleAsync(currentUser, "Administrator"))
            {
                var managers = await _userManager.GetUsersInRoleAsync("Kierownik");
                var employees = await _userManager.GetUsersInRoleAsync("Pracownik");
                allowedUsers.AddRange(managers);
                allowedUsers.AddRange(employees);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Kierownik"))
            {
                var employees = await _userManager.GetUsersInRoleAsync("Pracownik");
                allowedUsers.AddRange(employees);
            }

            var users = allowedUsers
                .Where(x => x.Id != currentUser.Id)
                .DistinctBy(x => x.Id)
                .ToList();
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
            if (model.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                ModelState.AddModelError(nameof(model.EndDate),
                    "Data rozpoczęcia nie może być wcześniejsza od dzisiejszej");
                return View(model);
            }
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
            if (!string.IsNullOrEmpty(user.Email))
            {
                var body = $@"
                <h2>Potwierdzenie złożenia wniosku urlopowego</h2>
                <p>Cześć {WebUtility.HtmlEncode(user.FirstName)},</p>
                <p>Twój wniosek został złożony i oczekuje na rozpatrzenie.</p>
                <ul>
                <li>Rodzaj: {WebUtility.HtmlEncode(leaveRequest.LeaveType)}</li>
                <li>Od: {leaveRequest.StartDate:dd.MM.yyyy}</li>
                <li>Do: {leaveRequest.EndDate:dd.MM.yyyy}</li>
                </ul>";
                await _emailService.SendEmailAsync(user.Email,
                    "Potwierdzenie złożenia wniosku urlopowego", body);
            }
            TempData["Success"] = "Pomyślnie dodano wniosek";
            return RedirectToAction("Index");
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

        
        
        [HttpPost, Authorize(Roles = "Kierownik,Administrator"), ValidateAntiForgeryToken]
        
        public async Task<IActionResult> ChangeRequestStatus(int id, string status, string? reason)
        {
            var request = await _context.LeaveRequests
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (request == null) return NotFound();

            if (request.Status != "Oczekujący")
            {
                TempData["Error"] = "Ten wniosek został już rozpatrzony.";
                return RedirectToAction("Index");
            }

            if (status != "Zaakceptowany" && status != "Odrzucony") return BadRequest();
            var approved = status == "Zaakceptowany";

            if (!approved && string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Podaj powód odrzucenia.";
                return RedirectToAction("Index");
            }

            var reviewer = await _userManager.GetUserAsync(User);
            request.Status = approved ? "Zaakceptowany" : "Odrzucony";
            request.ReviewerId = reviewer?.Id;
            request.ReviewerComment = approved ? null : reason!.Trim();
            request.DecisionDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.User.Email))
            {
                var comment = approved ? "" :
                $"<p>Komentarz kierownika: {WebUtility.HtmlEncode(request.ReviewerComment)}</p>";
                var body = $@"
                <h2>Decyzja w sprawie wniosku urlopowego</h2>
                <p>Cześć {WebUtility.HtmlEncode(request.User.FirstName)},</p>
                <p>Twój wniosek ({WebUtility.HtmlEncode(request.LeaveType)}, {request.StartDate:dd.MM.yyyy} – {request.EndDate:dd.MM.yyyy}) został <strong>{(approved ? "zaakceptowany" : "odrzucony")}</strong>.</p>
                {comment}";
                await _emailService.SendEmailAsync(request.User.Email,
                $"Wniosek urlopowy – {(approved ? "zaakceptowany" : "odrzucony")}", body);
            }

            TempData["Success"] = "Decyzja została zapisana";
            return RedirectToAction("Index");
        }
    }
}
