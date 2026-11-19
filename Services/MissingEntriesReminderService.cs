using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Services
{
    public class MissingEntriesReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MissingEntriesReminderService> _logger;
        private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

        public MissingEntriesReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<MissingEntriesReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);
                var next = now.Date.AddHours(9);
                if (next <= now)
                {
                    next = next.AddDays(1);
                }

                try
                {
                    await Task.Delay(next - now, stoppingToken);
                    
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await SendRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while sending missing entry reminders");
                }
            }
        }

        private async Task SendRemindersAsync(CancellationToken ct)
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone).Date;

            if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                return;
            }

            var checkedDay = today.DayOfWeek == DayOfWeek.Monday
                ? today.AddDays(-3)
                : today.AddDays(-1);

            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(checkedDay, _zone);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(checkedDay.AddDays(1), _zone);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var workers = (await userManager.GetUsersInRoleAsync("Pracownik"))
                .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                .ToList();

            var usersWithEntries = (await db.WorkEntries
                .Where(x => x.StartTime >= fromUtc && x.StartTime < toUtc)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet();

            foreach (var user in workers.Where(u => !usersWithEntries.Contains(u.Id)))
            {
                ct.ThrowIfCancellationRequested();

                var body = $@"
                    <h2>Brakujący wpis czasu pracy</h2>
                    <p>Cześć {WebUtility.HtmlEncode(user.FirstName)},</p>
                    <p>Nie znaleźliśmy Twojego wpisu czasu pracy z dnia {checkedDay:dd.MM.yyyy}.</p>
                    <p>Jeśli pracowałeś/aś, skontaktuj się z kierownikiem w celu korekty.</p>";

                await emailService.SendEmailAsync(
                    user.Email!, "Przypomnienie: brak wpisu czasu pracy", body);
            }

            _logger.LogInformation("Reminders for {Day:dd.MM.yyyy} processed", checkedDay);
        }
    }
}