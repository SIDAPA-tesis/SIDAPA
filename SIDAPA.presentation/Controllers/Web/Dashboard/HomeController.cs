using Microsoft.AspNetCore.Mvc;
using SIDAPA.presentation.Models.Dashboard;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class HomeController : Controller
    {
        public IActionResult index()
        {
            return View(HomeViewModel.Desde(User));
        }
    }
}
