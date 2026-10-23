using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class HomeController : Controller
    {
        public IActionResult index()
        {
            return View();
        }
    }
}
