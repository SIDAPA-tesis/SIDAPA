using SIDAPA.application.DTOs.Core.Notifications;

namespace SIDAPA.application.Interface.Core;

public interface INotificationService
{
    Task NotifyUserAsync(int userId, string message, int? referenciaId = null, bool sendEmail = true, CancellationToken ct = default);

    Task NotifyClientAsync(int clientId, string message, int? referenciaId = null, bool sendEmail = true, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(int userId, int take = 20, CancellationToken ct = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
    Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default);
}