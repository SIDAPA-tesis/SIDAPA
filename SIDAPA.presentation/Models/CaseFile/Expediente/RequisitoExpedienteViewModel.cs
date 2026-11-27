using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.CaseFile.Expediente;

public record RequisitoExpedienteViewModel(string Nombre, bool Obligatorio, string Etiqueta, string Color)
{
    // Un opcional que no llegó no es un problema: no se pinta como Faltante.
    public static RequisitoExpedienteViewModel Desde(ChecklistItemDto item) =>
        !item.IsMandatory && item.Status == DocumentStatusEnum.FALTANTE
            ? new(item.Name, false, "No recibido", "secondary")
            : new(item.Name, item.IsMandatory, item.Status.Etiqueta(), item.Status.ColorBootstrap());
}
