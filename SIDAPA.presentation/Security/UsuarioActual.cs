using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Security
{
    public class UsuarioActual : IUserActual
    {
        // Program.cs usa MapInboundClaims = false: los claims llegan con su nombre corto del token.
        private const string NameClaim = "name";
        private const string RoleClaim = "role";

        public string Nombre { get; }
        public IReadOnlyCollection<string> Roles { get; }

        public UsuarioActual(IHttpContextAccessor accesor, IConfiguration config, IWebHostEnvironment entorno)
        {
            var usuario = accesor.HttpContext?.User;

            if (usuario?.Identity?.IsAuthenticated == true)
            {
                Nombre = usuario.FindFirst(NameClaim)?.Value ?? "Usuario";
                Roles = usuario.FindAll(RoleClaim).Select(c => c.Value).ToList();
            }
            else if (entorno.IsDevelopment())
            {
                Nombre = config["UsuarioDemo:Nombre"] ?? "Usuario de prueba";
                Roles = [config["UsuarioDemo:Rol"] ?? RolesEnum.CAPTURADOR.ToString()];
            }
            else
            {
                Nombre = "Invitado";
                Roles = [];
            }
        }
    }

}
