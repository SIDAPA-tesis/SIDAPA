using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIDAPA.presentation.Models;
using SIDAPA.domain.Enums.Core;
using SIDAPA.application.Interface.Core;

namespace SIDAPA.presentation.ViewComponets
{
    public class MenuLateralViewComponet : ViewComponent
    {
        private static readonly Dictionary<ModuleEnum, (string Texto, string Controlador)> Catalogo = new()
        {
            [ModuleEnum.Inicio] = ("Inicio", "Home"),
            [ModuleEnum.Pedimentos] = ("Pedimentos", "Pedimentos"),
            [ModuleEnum.Claves] = ("Claves", "Claves"),
            [ModuleEnum.Reportes] = ("Reportes", "Reportes"),
            [ModuleEnum.Usuarios] = ("Usuarios", "Usuarios"),
        };

        private readonly IUserActual _usuario;
        private readonly IPermisosService _permisos;

        public MenuLateralViewComponet(IUserActual usuario, IPermisosService permisos)
        {
            _usuario = usuario;
            _permisos = permisos;
        }

        public IViewComponentResult Invoke()
        {
            var actual = ViewContext.RouteData.Values["controller"]?.ToString();

            var opciones = _permisos.ModulosPara(_usuario.Roles)
                .Select(m => new OpcionMenuViewModel(
                    Catalogo[m].Texto,
                    Catalogo[m].Controlador,
                    string.Equals(actual, Catalogo[m].Controlador, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return View(new MenuLateralViewModel(_usuario.Nombre, string.Join(", ", _usuario.Roles), opciones));
        }
    }
}
