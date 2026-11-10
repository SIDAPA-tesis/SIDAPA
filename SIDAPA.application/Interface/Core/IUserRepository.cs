using SIDAPA.domain.Entities.Core.Operative;

namespace SIDAPA.application.Interface.Core;

public interface IUserRepository
{
    /// <summary>Busca al usuario por correo (sin distinguir mayúsculas) e incluye su rol.</summary>
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task SaveLoginStateAsync(UserEntity user, CancellationToken ct = default);

    Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<UserEntity>> GetActiveByClientAsync(int clientId, CancellationToken ct = default);
}
