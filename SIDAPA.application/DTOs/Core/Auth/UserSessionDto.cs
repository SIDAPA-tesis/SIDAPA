using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Auth;

public record UserSessionDto(int UserId, string Name, string Email, RolesEnum Role, int? ClientId);