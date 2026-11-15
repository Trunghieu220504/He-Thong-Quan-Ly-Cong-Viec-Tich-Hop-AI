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
            var recipient = await identityApi.GetUserSummaryAsync(recipientUserId, ct);
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

            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gửi email thông báo qua MailHog thất bại cho UserId {UserId}.", recipientUserId);
        }
    }
}