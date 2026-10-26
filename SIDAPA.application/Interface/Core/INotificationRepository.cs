using System.Net.Http.Headers;
using SIDAPA.domain.Entities.Core.Operative;

namespace SIDAPA.application.Interface.Core;

public interface INotificationRepository
{
    Task AddAsync(NotificationEntity notification, CancellationToken ct = default);
    Task MarkEmailSentAsync(int notificationId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationEntity>> GetRecentAsync(int userId, int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default);
}