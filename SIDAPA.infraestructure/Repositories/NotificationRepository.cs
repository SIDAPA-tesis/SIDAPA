using Microsoft.EntityFrameworkCore;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

public class NotificationRepository(SidapaDbContext context) : INotificationRepository
{
    public async Task AddAsync(NotificationEntity notification, CancellationToken ct = default)
    {
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(ct);
    }

    public Task MarkEmailSentAsync(int notificationId, CancellationToken ct = default) => 
        context.Notifications
            .Where(x => x.Id == notificationId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsEmailSent, true), ct);

    public async Task<IReadOnlyList<NotificationEntity>> GetRecentAsync(int userId, int take, CancellationToken ct = default) =>
        await context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) => 
        context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default) =>
        await context.Notifications
            .Where(x => x.Id == notificationId && x.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct) > 0;
}