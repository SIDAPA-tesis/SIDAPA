using SIDAPA.application.DTOs.CaseFile.Documents;

namespace SIDAPA.presentation.Models.CaseFile.Documents;

/// <summary>Tarjeta "Documentos que te pedimos" del portal del Cliente (HU-05 / SCRUM-64).</summary>
/// <param name="Recibido">Confirmación de la última subida (viene de TempData).</param>
public record SolicitudesPendientesViewModel(IReadOnlyList<GrupoSolicitudesViewModel> Grupos, string? Recibido)
{
    public static SolicitudesPendientesViewModel Desde(
        IReadOnlyList<PendingDocumentRequestDto> pendientes, string? recibido, TimeZoneInfo zonaHoraria)
    {
        var grupos = pendientes
            .GroupBy(p => p.ReferenciaNumber)
            .Select(g => new GrupoSolicitudesViewModel(
                g.Key,
                g.Select(p => new SolicitudPendienteViewModel(
                        p.RequestId,
                        p.DocumentTypeName,
                        string.IsNullOrWhiteSpace(p.Note) ? null : p.Note,
                        HoraLocal(p.RequestedAt, zonaHoraria)))
                    .ToList()))
            .ToList();

        return new SolicitudesPendientesViewModel(grupos, recibido);
    }

    // La base guarda UTC sin zona.
    internal static DateTime HoraLocal(DateTime utc, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zona);
}
