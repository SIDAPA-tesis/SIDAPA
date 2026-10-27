using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.application.Interface.Core;

namespace SIDAPA.infraestructure.Email;

public class SmtpEmailSender(EmailOptions options, ILogger<SmtpEmailSender> logger): IEmailSender
{
    public async Task<bool> SendAsync(EmailMessageDto message, CancellationToken ct = default)
    {
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(options.From, options.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = true
            };
            mail.To.Add(message.To);

            using var client = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl,
                Credentials = string.IsNullOrEmpty(options.User)
                    ? null
                    : new NetworkCredential(options.User, options.Password)
            };

            await client.SendMailAsync(mail, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "No se pudo enviar el correo a {To}", message.To);
            return false;
            
        }
    }
}