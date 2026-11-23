using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;

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

            ClaveDto? seleccionada = null;
            if (!string.IsNullOrWhiteSpace(codigoSeleccionado))
            {
                var clave = await _repositorio.ObtenerAsync(codigoSeleccionado, ct);
                if (clave is not null) seleccionada = Mapear(clave);
            }

            var (vigentes, noVigentes) = await _repositorio.ContarAsync(ct);
            var metricas = new MetricasClavesDto(vigentes, noVigentes);

            return new TableroClavesDto(metricas, claves.Select(Mapear).ToList(), seleccionada);
        }

        private static ClaveDto Mapear(PedimentoKeyEntity c) => new(
            c.Code,
            c.Description,
            c.Group,
            c.Subgroup,
            c.ApplicationCases,
            c.IsActive,
            c.UpdatedOn);
    }
}
