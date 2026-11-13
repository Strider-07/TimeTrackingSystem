using Microsoft.Extensions.Options;
using TimeTrackingSystem.Data;
using TimeTrackingSystem.Models.Entities;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace TimeTrackingSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ApplicationDbContext _context;

        public EmailService(IOptions<EmailSettings> settings, ApplicationDbContext context)
        {
            _settings = settings.Value;
            _context = context;
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            
            
            var log = new EmailLog() { RecipientEmail = recipientEmail, Subject = subject, Body = body, SentAt = DateTime.UtcNow};

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", recipientEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(_settings.Username))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password);

                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                log.IsSuccess = true;
            }
            catch (Exception ex)
            {
                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;
            }

            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
            
        }

        

        
      
    }
}
