namespace TicketTracker.Application.Common.Interfaces;

/// <summary>Sends transactional email through the configured SMTP service.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
