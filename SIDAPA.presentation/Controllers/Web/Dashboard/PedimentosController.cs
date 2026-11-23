using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.presentation.Models;
using SIDAPA.presentation.Models.Pedimentos;
using System.Globalization;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class PedimentosController : Controller
    {
        private readonly IPedimentoService _servicio;

        public PedimentosController(IPedimentoService servicio) => _servicio = servicio;

        // Panel del capturador: cards de pedimentos + calendario de fechas límite.
        public async Task<IActionResult> Index(string? q, string? mes, CancellationToken ct)
        {
            var panel = await _servicio.ObtenerPanelAsync(q, ParsearMes(mes), ct);
            return View(PanelPedimentosViewModel.Desde(panel, q));
        }

        // Destino de las cards. Aquí va la vista completa del pedimento.
        public async Task<IActionResult> Detalle(int id, CancellationToken ct)
        {
            var pedimento = await _servicio.ObtenerAsync(id, ct);
            return pedimento is null ? NotFound() : View(pedimento);
        }

        private static DateOnly? ParsearMes(string? mes) =>
            DateOnly.TryParseExact($"{mes}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
                ? fecha
                : null;
    }
}
