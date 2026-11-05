using Microsoft.Extensions.Logging;
using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.application.Interface.Core;

namespace SIDAPA.infraestructure.Email;

public class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct = default)
    {
        logger.LogInformation("Correo SIMULADO para {To} · {Subject}{NewLine}{Body}",
            message.To, message.Subject, Environment.NewLine, message.Body);
        return Task.FromResult(true);
    }
}