using SIDAPA.application.DTOs.Core.Pedimento;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IPedimentoService
    {
        Task<PanelPedimentosDto> ObtenerPanelAsync(string? busqueda, DateOnly? mes, CancellationToken ct = default);
        Task<PedimentoDto?> ObtenerAsync(int id, CancellationToken ct = default);
    }

}
