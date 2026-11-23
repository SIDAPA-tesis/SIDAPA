using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Enums.Core;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.presentation.Models.Claves;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class ClavesController : Controller
    {
        private readonly IClaveService _servicio;

        public ClavesController(IClaveService servicio) => _servicio = servicio;

        public async Task<IActionResult> Index(string? q, ClaveTypeEnum? tipo, ClaveStateEnum? estado, string? clave, CancellationToken ct)
        {
            var tablero = await _servicio.ObtenerTableroAsync(new FiltroClaves(q, tipo, estado), clave, ct);
            return View(new ClavesViewModel(tablero, q, tipo, estado));
        }
    }
}
