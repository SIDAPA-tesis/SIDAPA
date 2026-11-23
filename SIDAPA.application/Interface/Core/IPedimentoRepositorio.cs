using SIDAPA.application.DTOs.Core.Pedimento;

namespace SIDAPA.application.Interface.Core
{
    public interface IPedimentoRepositorio
    {
        Task<IReadOnlyList<PedimentoResumenDto>> ListarAsync(string? busqueda, CancellationToken ct = default);
        Task<PedimentoResumenDto?> ObtenerAsync(int id, CancellationToken ct = default);
    }
}
