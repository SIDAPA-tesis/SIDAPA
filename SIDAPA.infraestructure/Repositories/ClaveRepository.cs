using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

/// <summary>Claves de pedimento (Apéndice 2) sobre <c>cat_clave_pedimento</c>.</summary>
public class ClaveRepository(SidapaDbContext context) : IClaveRepositorio
{
    public async Task<IReadOnlyList<PedimentoKeyEntity>> ListarAsync(FiltroClaves filtro, CancellationToken ct = default)
    {
        var query = context.PedimentoKeys.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var patron = $"%{filtro.Busqueda.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, patron)
                                     || EF.Functions.ILike(x.Description, patron)
                                     || EF.Functions.ILike(x.Group, patron));
        }

        if (filtro.Vigente is { } vigente)
            query = query.Where(x => x.IsActive == vigente);

        return await query.OrderBy(x => x.Code).ToListAsync(ct);
    }

    public Task<PedimentoKeyEntity?> ObtenerAsync(string codigo, CancellationToken ct = default) =>
        context.PedimentoKeys.AsNoTracking().FirstOrDefaultAsync(x => x.Code == codigo, ct);

    public async Task<(int Vigentes, int NoVigentes)> ContarAsync(CancellationToken ct = default)
    {
        var conteo = await context.PedimentoKeys
            .GroupBy(x => x.IsActive)
            .Select(g => new { Vigente = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        return (conteo.Where(c => c.Vigente).Sum(c => c.Total), conteo.Where(c => !c.Vigente).Sum(c => c.Total));
    }
}
