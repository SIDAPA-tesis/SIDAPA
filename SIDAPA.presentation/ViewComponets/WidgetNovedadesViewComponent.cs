using Microsoft.AspNetCore.Mvc;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.presentation.ViewComponets
{
    public class WidgetNovedadesViewComponent : ViewComponent
    {
        private readonly SidapaDbContext _context;
        public WidgetNovedadesViewComponent(SidapaDbContext context)
        {
            _context = context;
        }
        //public async Task<IViewComponentResult> InvokeAsync()
        //{
        //    var novedades = await _context.
        //}
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
