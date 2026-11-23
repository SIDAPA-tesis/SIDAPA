using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.application.Interface.Core;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

public class CatalogRepository(SidapaDbContext context) : ICatalogRepository
{
    public async Task<IReadOnlyList<AduanaOptionDto>> GetActiveAduanasAsync(CancellationToken ct = default) =>
        await context.Aduanas
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AduanaOptionDto(x.Code, x.Name))
            .ToListAsync(ct);

    public Task<bool> IsActiveAduanaAsync(string aduanaCode, CancellationToken ct = default) =>
        context.Aduanas.AnyAsync(x => x.Code == aduanaCode && x.IsActive, ct);
}
