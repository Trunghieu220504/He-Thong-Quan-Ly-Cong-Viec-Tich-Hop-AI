using System.Net.Mail;
using Identity.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Email;

public sealed class SmtpNotificationEmailSender(
    IConfiguration configuration,
    IIdentityModuleApi identityApi,
    ILogger<SmtpNotificationEmailSender> logger) : INotificationEmailSender
{
    public async Task SendTaskAssignedEmailAsync(
        Guid recipientUserId,
        string taskTitle,
        string assignedByDisplayName,
        Guid projectId,
        Guid taskId,
        CancellationToken ct = default)
    {
        try
        {
            if (!int.TryParse(configuration["Smtp:TimeoutSeconds"] ?? "5", out var timeoutSeconds)
                || timeoutSeconds <= 0)
                throw new InvalidOperationException("Smtp:TimeoutSeconds must be positive.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var recipient = await identityApi.GetUserSummaryAsync(recipientUserId, timeout.Token);
            if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
                return;

            var smtpHost = configuration["Smtp:Host"] ?? "localhost";
            var smtpPort = int.TryParse(configuration["Smtp:Port"], out var port) ? port : 1025;
            var fromAddress = configuration["Smtp:From"] ?? "noreply@pmai.local";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var message = new MailMessage(fromAddress, recipient.Email)
            {
                Subject = $"[PMAI] Bạn được giao công việc: {taskTitle}",
                Body = $"Xin chào {recipient.DisplayName},\n\n{assignedByDisplayName} đã giao công việc \"{taskTitle}\" cho bạn."
            };

            await client.SendMailAsync(message, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gửi email thông báo qua MailHog thất bại cho UserId {UserId}.", recipientUserId);
        }
    }
}
