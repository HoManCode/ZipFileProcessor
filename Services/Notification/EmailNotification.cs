using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZipFileProcessor.Services.Notification;

public class EmailNotification : INotification
{
    private readonly string _smtpServer;
    private readonly string _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _adminEmail;
    private readonly ILogger<EmailNotification> _logger;
        
    public EmailNotification(IConfiguration configuration, ILogger<EmailNotification> logger)
    {
        _smtpServer = configuration["EmailSettings:SmtpServer"];
        _smtpPort = configuration["EmailSettings:SmtpPort"];
        _smtpUsername = configuration["EmailSettings:SmtpUsername"];
        _smtpPassword = configuration["EmailSettings:SmtpPassword"];
        _adminEmail = configuration["EmailSettings:AdminEmail"];
        _logger = logger;
    }
    
    public async Task SendNotification(string subject, string message)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUsername),
                Subject = subject,
                Body = message,
                IsBodyHtml = false
            };
            mailMessage.To.Add(_adminEmail);

            using var smtpClient = new SmtpClient(_smtpServer, int.Parse(_smtpPort))
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (FormatException ex)
        {
            _logger.LogError("Format exception: {ex.Message}", ex.Message);
        }
        catch (SmtpException ex)
        {
            _logger.LogError("SMTP exception: {ex.Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception: {ex.Message}", ex.Message);
        }
        
    }
}