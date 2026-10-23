using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Models;

namespace SIDAPA.presentation.Controllers.Web.SIDAPA;

public class IndexController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {

        var hora = DateTime.Now.Hour;
        var saludo = hora < 12 ? "Buenos días" : hora < 19 ? "Buenas Tardes" : "Buenas Noches";

        var nombre = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = "Usuario";

        // datos de prueba luego me ayudan a linkear lo demas
        var pendientes = new List<Pendiente>
        {
            new("Revisar solitudes nuevas", "hoy"),
            new("Validar registros", "Mañana"),
            new("Enviar reporte mensual", "viernes"),
        };
        return View("~/Views/SIDAPA/Index.cshtml",
                    new InicioViewModel($"{saludo}, {nombre}", pendientes));
    }
}