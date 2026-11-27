using System.Globalization;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.presentation.Models.CaseFile.Documents;

namespace SIDAPA.presentation.Models.CaseFile.Expediente;

public record BandejaExpedientesViewModel(
    IReadOnlyList<FilaExpedienteViewModel> Filas, string? Busqueda, bool PorRevisar, int Pagina, int TotalPaginas, int Total)
{
    public static BandejaExpedientesViewModel Desde(
        ExpedientePageDto pagina, string? busqueda, bool porRevisar, TimeZoneInfo zonaHoraria) =>
        new(
            pagina.Items.Select(r => new FilaExpedienteViewModel(r, SolicitudesPendientesViewModel.HoraLocal(r.CreatedAt, zonaHoraria))).ToList(),
            busqueda,
            porRevisar,
            pagina.Page,
            pagina.TotalPages,
            pagina.TotalCount);

    /// <summary>Liga a otra página conservando la búsqueda y el filtro.</summary>
    public Dictionary<string, string> Ruta(int pagina)
    {
        var ruta = new Dictionary<string, string> { ["pagina"] = pagina.ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrWhiteSpace(Busqueda))
            ruta["q"] = Busqueda;
        if (PorRevisar)
            ruta["porRevisar"] = "true";
        return ruta;
    }

    // A lo más 5 números alrededor de la página actual; el resto se alcanza con Anterior/Siguiente.
    public IEnumerable<int> PaginasVisibles
    {
        get
        {
            var desde = Math.Max(1, Math.Min(Pagina - 2, TotalPaginas - 4));
            return Enumerable.Range(desde, Math.Min(5, TotalPaginas));
        }
    }
}
