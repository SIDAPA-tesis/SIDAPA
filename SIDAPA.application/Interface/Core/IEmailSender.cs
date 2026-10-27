using SIDAPA.application.DTOs.Core.Notifications;

namespace SIDAPA.application.Interface.Core;

public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessageDto message , CancellationToken ct = default);
}