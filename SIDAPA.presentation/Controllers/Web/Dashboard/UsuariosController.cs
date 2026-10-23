using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Models.Dashboard;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class UsuariosController : Controller
    {
        public IActionResult Index()
        {
            var usuarios = new List<Usuario>
        {
            new(1, "Ana Torres",      "ana.torres@ejemplo.com",      "Administrador", true),
            new(2, "Luis Hernández",  "luis.hernandez@ejemplo.com",  "Editor",        true),
            new(3, "María Gómez",     "maria.gomez@ejemplo.com",     "Consulta",      false),
            new(4, "Carlos Ruiz",     "carlos.ruiz@ejemplo.com",     "Editor",        true),
            new(5, "Sofía Medina",    "sofia.medina@ejemplo.com",    "Consulta",      true),
        };

            return View(usuarios);
        }
    }
}
