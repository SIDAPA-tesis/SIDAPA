using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.Interface.Core;
using SIDAPA.presentation.Models.Claves;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    [Authorize(Policy = Policies.Glosa)]
    public class ClavesController : Controller
    {
        private readonly IClaveService _servicio;

        public ClavesController(IClaveService servicio) => _servicio = servicio;

        public async Task<IActionResult> Index(string? q, bool? vigente, string? clave, CancellationToken ct)
        {
            var tablero = await _servicio.ObtenerTableroAsync(new FiltroClaves(q, vigente), clave, ct);
            return View(new ClavesViewModel(tablero, q, vigente));
        }
    }
}
