using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.presentation.ViewComponets
{
    public class WidgetAccesosRapidosViewComponet : ViewComponent
    {
        private readonly SidapaDbContext _context;
        public WidgetAccesosRapidosViewComponet(SidapaDbContext context)
        {
            _context = context;
        }
        //public async Task<IViewComponentResult> InvokeAsync()
        //{
        //    var accesos = await _context.
        //}
        public IViewComponentResult Invoke()
        {
            return View();
        } 
    }
}
