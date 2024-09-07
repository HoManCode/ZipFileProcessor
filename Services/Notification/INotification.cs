namespace ZipFileProcessor.Services.Notification;

public interface INotification
{
    Task SendNotification(string subject, string message);
}