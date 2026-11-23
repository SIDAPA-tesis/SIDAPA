using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;

namespace SIDAPA.application.Interface.Core
{
    /// <summary>Claves de pedimento del Apéndice 2 (tabla <c>cat_clave_pedimento</c>).</summary>
    public interface IClaveRepositorio
    {
        Task<IReadOnlyList<PedimentoKeyEntity>> ListarAsync(FiltroClaves filtro, CancellationToken ct = default);
        Task<PedimentoKeyEntity?> ObtenerAsync(string codigo, CancellationToken ct = default);
        Task<(int Vigentes, int NoVigentes)> ContarAsync(CancellationToken ct = default);
    }
}
