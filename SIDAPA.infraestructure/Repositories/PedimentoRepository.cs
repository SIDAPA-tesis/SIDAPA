using Microsoft.EntityFrameworkCore;
using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.infraestructure.Persistence;

namespace SIDAPA.infraestructure.Repositories;

/// <summary>
/// Panel de pedimentos: lectura de <c>ope_referencia</c> + cliente. La fecha límite es la fecha
/// estimada de arribo (o la de creación si el cliente no la indicó).
/// </summary>
public class PedimentoRepository(SidapaDbContext context) : IPedimentoRepositorio
{
    public async Task<IReadOnlyList<PedimentoResumenDto>> ListarAsync(string? busqueda, CancellationToken ct = default)
    {
        var query = context.Referencias.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.ReferenciaNumber, patron)
                                     || EF.Functions.ILike(x.Client.BusinessName, patron));
        }

        var filas = await Proyectar(query.OrderByDescending(x => x.CreatedAt)).ToListAsync(ct);
        return filas.Select(ToDto).ToList();
    }

    public async Task<PedimentoResumenDto?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var fila = await Proyectar(context.Referencias.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync(ct);
        return fila is null ? null : ToDto(fila);
    }

    private static IQueryable<Fila> Proyectar(IQueryable<ReferenciaEntity> query) =>
        query.Select(x => new Fila(x.Id, x.ReferenciaNumber, x.Client.BusinessName, x.StatusId, x.ArrivalDate, x.CreatedAt));

    private static PedimentoResumenDto ToDto(Fila f) =>
        new(f.Id, f.Numero, f.Empresa, null, f.StatusId, f.ArrivalDate ?? DateOnly.FromDateTime(f.CreatedAt));

    private sealed record Fila(int Id, string Numero, string Empresa, short StatusId, DateOnly? ArrivalDate, DateTime CreatedAt);
}
