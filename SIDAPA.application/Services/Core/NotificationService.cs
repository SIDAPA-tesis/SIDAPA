using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.Services.Core;

public class NotificationService(
    INotificationRepository notifications,
    IUserRepository users,
    IEmailSender emailSender) : INotificationService
{
    private const string EmailSubject = "SIDAPA · Tienes un nuevo aviso";

    public async Task NotifyUserAsync(int userId, string message, int? referenciaId = null,
        bool sendEmail = true, CancellationToken ct = default)
    {
        var text = NotificationRule.EnsureValidMessage(message);
        var user = await users.GetByIdAsync(userId, ct) ?? throw new UserNotFoundException();

        await NotifyAsync(user, text, referenciaId, sendEmail, ct);
    }

    public async Task NotifyClientAsync(int clientId, string message, int? referenciaId = null,
        bool sendEmail = true, CancellationToken ct = default)
    {
        var text = NotificationRule.EnsureValidMessage(message);
        var clientUsers = await users.GetActiveByClientAsync(clientId, ct);

        foreach (var user in clientUsers)
            await NotifyAsync(user, text, referenciaId, sendEmail, ct);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int userId, int take = 20, CancellationToken ct = default)
    {
        var list = await notifications.GetRecentAsync(userId, take, ct);
        return list.Select(n => new NotificationDto(n.Id, n.Message, n.ReferenciaId, n.IsRead, n.CreatedAt)).ToList();
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
        notifications.CountUnreadAsync(userId, ct);

    public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default)
    {
        if (!await notifications.MarkAsReadAsync(userId, notificationId, ct))
            throw new NotificationNotFoundException();
    }

    // 1) Guarda el aviso (panel). 2) Si se pidió, manda el correo y lo marca como enviado.
    // Si el correo falla, el aviso ya quedó guardado.
    private async Task NotifyAsync(UserEntity user, string text, int? referenciaId, bool sendEmail, CancellationToken ct)
    {
        var notification = new NotificationEntity
        {
            UserId = user.Id,
            ReferenciaId = referenciaId,
            Message = text,
            IsRead = false,
            IsEmailSent = false,
            CreatedAt = UtcNow()
        };
        await notifications.AddAsync(notification, ct);

        if (!sendEmail)
            return;

        var email = new EmailMessageDto(user.Email, EmailSubject, BuildEmailBody(user.Name, text));
        if (await emailSender.SendAsync(email, ct))
            await notifications.MarkEmailSentAsync(notification.Id, ct);
    }

    private static string BuildEmailBody(string name, string text) =>
        $"""
        Hola {name}:

        {text}

        Entra a SIDAPA para ver el detalle.

        Este correo se envió automáticamente; no lo respondas.
        """;

    // Igual que en UserService: la columna es "timestamp without time zone".
    private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}