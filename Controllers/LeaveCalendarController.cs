using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TimeTrackingSystem.Data;

namespace TimeTrackingSystem.Controllers;

[Authorize]
public class LeaveCalendarController : Controller
{
    private const string ApprovedStatus = "Zaakceptowany";
    private const string PendingStatus = "Oczekujący";

    private readonly ApplicationDbContext _context;

    public LeaveCalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] string? status)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return BadRequest("Nie podano zakresu dat.");
        }

        if (end.Value <= start.Value)
        {
            return BadRequest("Nieprawidłowy zakres dat.");
        }

        
        DateOnly startDate =
            DateOnly.FromDateTime(start.Value.DateTime);

        DateOnly endDate =
            DateOnly.FromDateTime(end.Value.DateTime);

        bool canSeePending =
            User.IsInRole("Administrator") ||
            User.IsInRole("Kierownik");

        
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.Status == ApprovedStatus);

        
        if (canSeePending)
        {
            query = _context.LeaveRequests
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r =>
                    r.Status == ApprovedStatus ||
                    r.Status == PendingStatus);
        }

        
        if (status == ApprovedStatus)
        {
            query = query.Where(r => r.Status == ApprovedStatus);
        }
        else if (status == PendingStatus)
        {
            if (!canSeePending)
            {
                return Forbid();
            }

            query = query.Where(r => r.Status == PendingStatus);
        }
        else if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            return BadRequest("Nieprawidłowy filtr statusu.");
        }

        
        query = query.Where(r =>
            r.StartDate < endDate &&
            r.EndDate >= startDate);

        var requests = await query
            .OrderBy(r => r.StartDate)
            .ToListAsync();

        var events = requests.Select(request =>
        {
            string employeeName;

            if (request.User == null)
            {
                employeeName = "Nieznany pracownik";
            }
            else
            {
                employeeName =
                    $"{request.User.FirstName} {request.User.LastName}".Trim();

                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    employeeName =
                        request.User.Email ?? "Nieznany pracownik";
                }
            }

            bool isApproved = request.Status == ApprovedStatus;

            return new
            {
                id = request.Id,

                title = $"{employeeName} – {request.LeaveType}",

                start = request.StartDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                
                end = request.EndDate.AddDays(1).ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                allDay = true,

                backgroundColor = isApproved ? "#198754" : "#ffc107",
                borderColor = isApproved ? "#198754" : "#ffc107",
                textColor = isApproved ? "#ffffff" : "#212529",

                extendedProps = new
                {
                    employeeName,
                    leaveType = request.LeaveType,
                    status = request.Status,

                    
                    reason = canSeePending ? request.Reason : null,

                    startDate = request.StartDate.ToString(
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture),

                    endDate = request.EndDate.ToString(
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture),

                    
                    reviewerComment = canSeePending
                        ? request.ReviewerComment
                        : null
                }
            };
        });

        return Ok(events);
    }
}