using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Models.Dashboard;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
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
