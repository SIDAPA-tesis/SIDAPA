using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.application.Interface.Core;

namespace SIDAPA.infraestructure.Security;

public class JwtTokenService(JwtOptions options) : ITokenService
{
    public AuthToken CreateToken(UserSessionDto user)
    {
        var expires = DateTime.UtcNow.AddMinutes(options.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString()),   // nombre del enum: "CAPTURADOR", "GLOSADOR"…
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (user.ClientId is int clientId)
            claims.Add(new Claim("client_id", clientId.ToString()));   // solo para el rol Cliente

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthToken(token, expires);
    }
}