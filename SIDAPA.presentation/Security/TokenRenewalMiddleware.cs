using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Security;

/// <summary>
/// Si el usuario sigue activo y a su token le queda menos de la mitad de vida, le entrega uno nuevo.
/// </summary>
public class TokenRenewalMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated == true
            && long.TryParse(user.FindFirst("exp")?.Value, out var exp)
            && long.TryParse(user.FindFirst("iat")?.Value, out var iat))
        {
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
            var halfLife = (expiresAt - DateTimeOffset.FromUnixTimeSeconds(iat)) / 2;

            if (expiresAt - DateTimeOffset.UtcNow < halfLife)
            {
                var session = new UserSessionDto(
                    int.Parse(user.FindFirst("sub")!.Value),
                    user.FindFirst("name")!.Value,
                    user.FindFirst("email")!.Value,
                    Enum.Parse<RolesEnum>(user.FindFirst("role")!.Value, ignoreCase: true),
                    int.TryParse(user.FindFirst("client_id")?.Value, out var clientId) ? clientId : null);

                AuthCookie.Write(context.Response, tokenService.CreateToken(session));
            }
        }

        await next(context);
    }
}