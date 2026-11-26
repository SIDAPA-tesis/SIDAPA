using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Security;
using SIDAPA.presentation.Models.Dashboard;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    [Authorize(Policy = Policies.Administration)]   // HU-23 (docs/permisos-por-rol.md)
    public class ReportesController : Controller
    {
        public IActionResult Index()
        {
            var altas = new List<ReporteMensual>
            {
                new("Ene", 12),
                new("Feb", 18),
                new("Mar", 9),
                new("Abr", 10),
                new("May", 20)
            };

            return View(altas);
        }
    }
}
