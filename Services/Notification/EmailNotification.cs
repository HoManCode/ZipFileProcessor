using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ZipFileProcessor.Services.Notification;

public class EmailNotification : INotification
{
    private readonly string _smtpServer;
    private readonly string _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _adminEmail;
        
    public EmailNotification(IConfiguration configuration)
    {
        _smtpServer = configuration["EmailSettings:SmtpServer"];
        _smtpPort = configuration["EmailSettings:SmtpPort"];
        _smtpUsername = configuration["EmailSettings:SmtpUsername"];
        _smtpPassword = configuration["EmailSettings:SmtpPassword"];
        _adminEmail = configuration["EmailSettings:AdminEmail"];
    }
    
    public async Task SendNotification(string subject, string message)
    {
        Log.Information("********************Starting email SendNotification method********************");
        
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

            using var smtpClient = new SmtpClient(_smtpServer, int.Parse(_smtpPort));
            smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (FormatException ex)
        {
            Log.Error("Format exception: {Message}", ex.Message);
        }
        catch (SmtpException ex)
        {
            Log.Error("SMTP exception: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Exception: {Message}", ex.Message);
        }
        
    }
}