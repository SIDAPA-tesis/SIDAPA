using SIDAPA.application.DTOs.Core.Claves;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IClaveService
    {
        Task<TableroClavesDto> ObtenerTableroAsync(FiltroClaves filtro, string? codigoSeleccionado, CancellationToken ct = default);
    }
}
