using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

public class UserRepository(SidapaDbContext context) : IUserRepository
{
    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToLowerInvariant();

        return context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalized, ct);
    }

    public Task SaveLoginStateAsync(UserEntity user, CancellationToken ct = default) =>
        context.Users
            .Where(x => x.Id == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.FailedLoginAttempts, user.FailedLoginAttempts)
                .SetProperty(x => x.LockedUntil, user.LockedUntil), ct);

    public Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
    context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<UserEntity>> GetActiveByClientAsync(int clientId, CancellationToken ct = default) =>
        await context.Users
            .AsNoTracking()
            .Where(x => x.ClientId == clientId && x.IsActive)
            .ToListAsync(ct);
}
