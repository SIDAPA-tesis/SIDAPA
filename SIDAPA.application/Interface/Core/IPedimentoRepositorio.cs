using SIDAPA.domain.Entities.Core.Catalogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IPedimentoRepositorio
    {
        Task<IReadOnlyList<PedimentoTypeEntity>> ListarAsync(string? busqueda, CancellationToken ct = default);
        Task<PedimentoTypeEntity?> ObtenerAsync(int id, CancellationToken ct = default);
    }
}
