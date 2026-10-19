using SIDAPA.application.DTOs.Core.Auth;

namespace SIDAPA.application.Interface.Core;

public interface IUserService
{
    /// <summary>Valida correo y contraseña y regresa los datos de la sesión.</summary>
    /// <exception cref="SIDAPA.domain.Exceptions.CoreException.InvalidCredentialsException">Si no son correctos.</exception>
    Task<UserSessionDto> LoginAsync(string email, string password, CancellationToken ct = default);
}
