using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.CaseFile.Documents;

namespace SIDAPA.presentation.Models.CaseFile.Expediente;

/// <summary>
/// Detalle del expediente (HU-03). Lo enlazan el GET (<c>/Expedientes/Detalle/7?documento=3</c>) y el POST que marca un
/// documento; el resto lo llena <see cref="Completar"/>, para que <c>DomainExceptionFilter</c> pueda volver a pintarlo.
/// </summary>
public class ExpedienteDetalleViewModel
{
    public int Id { get; set; }
    public int? Documento { get; set; }
    public int DocumentoMarcado { get; set; }
    public DocumentStatusEnum Estado { get; set; }
    public string? Nota { get; set; }

    [BindNever] public ExpedienteDetailDto Referencia { get; private set; } = null!;
    [BindNever] public DateTime Recibida { get; private set; }
    [BindNever] public IReadOnlyList<DocumentoRecibidoViewModel> Documentos { get; private set; } = [];
    [BindNever] public IReadOnlyList<RequisitoExpedienteViewModel> Requisitos { get; private set; } = [];
    [BindNever] public int ObligatoriosCorrectos { get; private set; }
    [BindNever] public int Obligatorios { get; private set; }
    [BindNever] public bool EnRevision { get; private set; }
    [BindNever] public bool PuedeMarcar { get; private set; }
    [BindNever] public DocumentPreviewDto? VistaPrevia { get; private set; }
    [BindNever] public string? Mensaje { get; set; }

    /// <param name="puedeMarcar">El usuario tiene la política Capture; el estatus se revisa aquí.</param>
    public ExpedienteDetalleViewModel Completar(
        ExpedienteReviewDto review, DocumentPreviewDto? vistaPrevia, bool puedeMarcar, TimeZoneInfo zonaHoraria)
    {
        Referencia = review.Detail;
        Recibida = SolicitudesPendientesViewModel.HoraLocal(review.Detail.CreatedAt, zonaHoraria);
        EnRevision = review.CanBeReviewed;
        PuedeMarcar = puedeMarcar && review.CanBeReviewed;

        // Un ?documento= de otra referencia no se abre aquí.
        VistaPrevia = vistaPrevia?.ReferenciaId == review.Detail.Id ? vistaPrevia : null;

        Documentos = review.Detail.Documents
            .Select(d => new DocumentoRecibidoViewModel(
                review.Detail.Id, d.Id, d.TypeName, d.FileName, Tamano(d.SizeInBytes), d.Status, d.UploadedByName,
                SolicitudesPendientesViewModel.HoraLocal(d.UploadedAt, zonaHoraria),
                d.Id == VistaPrevia?.DocumentId, PuedeMarcar, VistaPrevia?.DocumentId, d.Note))
            .ToList();

        Requisitos = review.Checklist.Select(RequisitoExpedienteViewModel.Desde).ToList();
        Obligatorios = review.Checklist.Count(c => c.IsMandatory);
        ObligatoriosCorrectos = review.Checklist.Count(c => c.IsMandatory && c.Status == DocumentStatusEnum.CORRECTO);
        return this;
    }

    public static string Confirmacion(ExpedienteStatusDto resultado, DocumentStatusEnum marca)
    {
        if (resultado.StatusChanged)
            return "Todos los documentos obligatorios están correctos: la referencia pasó a Expediente completo.";

        var faltan = resultado.MissingDocuments.Count;
        var marcado = $"Documento marcado como {marca.Etiqueta()}.";
        return faltan switch
        {
            0 => marcado,
            1 => $"{marcado} Falta 1 documento obligatorio por marcar como correcto.",
            _ => $"{marcado} Faltan {faltan} documentos obligatorios por marcar como correctos."
        };
    }

    private static string Tamano(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => (bytes / 1024d).ToString("0.#", CultureInfo.InvariantCulture) + " KB",
        _ => (bytes / (1024d * 1024)).ToString("0.#", CultureInfo.InvariantCulture) + " MB"
    };
}
