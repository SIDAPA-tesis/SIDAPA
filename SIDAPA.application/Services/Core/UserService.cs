using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;

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

        // Usuario inactivo: mismo error genérico, no se revela que la cuenta existe.
        if (!passwordHasher.Verify(user.PasswordHash, password) || !user.IsActive)
            throw new InvalidCredentialsException();

        return new UserSessionDto(user.Id, user.Name, user.Email, (RolesEnum)user.RoleId, user.ClientId);
    }
}
