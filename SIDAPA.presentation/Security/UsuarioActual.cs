using System.Security.Claims;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIDAPA.presentation.Security
{
    public class UsuarioActual : IUserActual
    {
        public string Nombre { get; }
        public IReadOnlyCollection<string> Roles { get; }

        public UsuarioActual(IHttpContextAccessor accesor, IConfiguration config, IWebHostEnvironment entorno)
        {
            var usuario = accesor.HttpContext?.User;

            if (usuario?.Identity?.IsAuthenticated == true)
            {
                Nombre = usuario.Identity!.Name ?? "Usuario";
                Roles = usuario.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
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
