using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Catalogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIDAPA.application.Services.Core
{
    public class ClaveService : IClaveService
    {
        private readonly IClaveRepositorio _repositorio;

        public ClaveService(IClaveRepositorio repositorio) => _repositorio = repositorio;

        public async Task<TableroClavesDto> ObtenerTableroAsync(
            FiltroClaves filtro, string? codigoSeleccionado, CancellationToken ct = default)
        {
            var claves = await _repositorio.ListarAsync(filtro, ct);
            var conteo = await _repositorio.ContarPorEstadoAsync(ct);

            ClaveDto? seleccionada = null;
            if (!string.IsNullOrWhiteSpace(codigoSeleccionado))
            {
                var clave = await _repositorio.ContarAsync(codigoSeleccionado, ct);
                if (clave is not null) seleccionada = Mapear(clave);
            }

            var (vigentes, noVigentes) = await _repositorio.ContarAsync(ct);
            var metricas = new MetricasClavesDto(vigentes, noVigentes);

            return new TableroClavesDto(metricas, claves.Select(Mapear).ToList(), seleccionada);
        }

        private static ClaveDto Mapear(ClaveTypeEntity c) => new(
            c.Codigo,
            c.Descripcion,
            c.Grupo,
            c.SubGrupo,
            c.Supuestos,
            c.Vigente,
            c.FechaActualizacion);
    }
}