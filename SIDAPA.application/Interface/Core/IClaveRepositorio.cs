using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.Services.Core;
using SIDAPA.domain.Enums.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.application.Interface.Core
{
    public interface IClaveRepositorio
    {
        Task<IReadOnlyList<ClaveTypeEntity>> ListarAsync(FiltroClaves filtro, CancellationToken ct = default);
        Task<(int Vigentes, int NoVigentes)> ContarAsync(CancellationToken ct = default);
        Task<IReadOnlyDictionary<ClaveStateEnum, int>> ContarPorEstadoAsync(CancellationToken ct = default);

    }
}
