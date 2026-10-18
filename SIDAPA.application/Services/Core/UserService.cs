using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.Services.Core;

public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher) : IUserService
{
    // Hash de relleno: si el correo no existe se verifica contra él para que la respuesta
    // tarde lo mismo y no se pueda saber qué correos están registrados. Se calcula una sola vez.
    private static string? _dummyHash;

    public async Task<UserSessionDto> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email.Trim(), ct);

        if (user is null)
        {
            _dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            passwordHasher.Verify(_dummyHash, password);
            throw new InvalidCredentialsException();
        }

        var now = UtcNow();
        var hadFailures = user.FailedLoginAttempts > 0 || user.LockedUntil is not null;

        // Bloqueada: ni con la contraseña correcta entra.
        LoginLockoutRule.EnsureNotLocked(user, now);

        if (!passwordHasher.Verify(user.PasswordHash, password))
        {
            LoginLockoutRule.RegisterFailure(user, now);
            await userRepository.SaveLoginStateAsync(user, ct);

            // Si este fue el 5º intento, avisa que quedó bloqueada.
            LoginLockoutRule.EnsureNotLocked(user, now);
            throw new InvalidCredentialsException();
        }

        // Usuario inactivo: mismo error genérico, no se revela que la cuenta existe.
        if (!user.IsActive)
            throw new InvalidCredentialsException();

        if (hadFailures)
        {
            LoginLockoutRule.RegisterSuccess(user);
            await userRepository.SaveLoginStateAsync(user, ct);
        }

        return new UserSessionDto(user.Id, user.Name, user.Email, (RolesEnum)user.RoleId, user.ClientId);
    }

    // La columna es "timestamp without time zone": se guarda la hora UTC sin marca de zona,
    // porque Npgsql no acepta escribir un DateTime con Kind=Utc en ese tipo de columna.
    private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}