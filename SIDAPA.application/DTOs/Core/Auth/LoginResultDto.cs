namespace SIDAPA.application.DTOs.Core.Auth;

/// <summary>Resultado del caso de uso de inicio de sesión: la sesión si tuvo éxito o el motivo del error.</summary>
public record LoginResultDto(UserSessionDto? Session, LoginErrorEnum? Error)
{
    public bool Succeeded => Session is not null;

    public static LoginResultDto Success(UserSessionDto session) => new(session, null);

    public static LoginResultDto Failure(LoginErrorEnum error) => new(null, error);
}
