using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIDAPA.application.DTOs.CaseFile.Documents;

namespace SIDAPA.presentation.Models.CaseFile.Documents;

/// <summary>Página "Subir documento solicitado" (HU-05): un solo archivo para una sola solicitud.</summary>
public class SubirSolicitudViewModel
{
    /// <summary>Viene en la URL (?requestId=) y en el formulario. Que sea del cliente lo revisa el servicio.</summary>
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Elige el archivo que te pedimos.")]
    [Display(Name = "Archivo")]
    public IFormFile? Archivo { get; set; }

    // Datos de la solicitud: los llena el controller, no vienen en el POST.
    [BindNever] public bool Disponible { get; private set; }
    [BindNever] public string ReferenciaNumber { get; private set; } = "";
    [BindNever] public string Documento { get; private set; } = "";
    [BindNever] public string? Nota { get; private set; }
    [BindNever] public DateTime Solicitada { get; private set; }

    public SubirSolicitudViewModel Completar(DocumentRequestDetailDto solicitud, TimeZoneInfo zonaHoraria)
    {
        Disponible = true;
        ReferenciaNumber = solicitud.ReferenciaNumber;
        Documento = solicitud.DocumentTypeName;
        Nota = string.IsNullOrWhiteSpace(solicitud.Note) ? null : solicitud.Note;
        Solicitada = SolicitudesPendientesViewModel.HoraLocal(solicitud.RequestedAt, zonaHoraria);
        return this;
    }

    /// <summary>Mensaje para el cliente después de subir el archivo.</summary>
    public static string Confirmacion(DocumentRequestAttendedDto atendida)
    {
        var recibido = $"Recibimos \"{atendida.DocumentTypeName}\" de la referencia {atendida.ReferenciaNumber}.";

        if (atendida.Remaining == 0)
            return $"{recibido} Ya tenemos todo lo que te pedimos; tu referencia regresó a revisión.";

        return atendida.Remaining == 1
            ? $"{recibido} Aún falta 1 documento."
            : $"{recibido} Aún faltan {atendida.Remaining} documentos.";
    }
}
