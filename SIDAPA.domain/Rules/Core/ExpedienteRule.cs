using SIDAPA.domain.Enums.Core;

namespace SIDAPA.domain.Rules.Core;

/// <summary>
/// HU-03: el expediente está completo cuando cada documento obligatorio de la operación
/// tiene al menos un archivo marcado como CORRECTO.
/// </summary>
public static class ExpedienteRule
{
    /// <summary>Documentos obligatorios que todavía no tienen ningún archivo CORRECTO.</summary>
    public static IReadOnlyList<DocumentTypeEnum> MissingDocuments(
        IEnumerable<DocumentTypeEnum> mandatory,
        IEnumerable<(DocumentTypeEnum Type, DocumentStatusEnum Status)> documents)
    {
        var correct = documents
            .Where(d => d.Status == DocumentStatusEnum.CORRECTO)
            .Select(d => d.Type)
            .ToHashSet();

        return mandatory.Distinct().Where(type => !correct.Contains(type)).ToList();
    }

    /// <summary>
    /// Si la operación no tiene documentos obligatorios configurados, nunca se da por completo sola:
    /// es mejor que lo revise una persona a cerrar un expediente vacío por error.
    /// </summary>
    public static bool IsComplete(IReadOnlyCollection<DocumentTypeEnum> mandatory, IReadOnlyCollection<DocumentTypeEnum> missing) =>
        mandatory.Count > 0 && missing.Count == 0;

    /// <summary>
    /// Estado de un renglón de la lista de verificación. Sin archivo es FALTANTE; con varios archivos del mismo tipo
    /// gana CORRECTO > RECIBIDO (falta revisarlo) > ERROR > FALTANTE: basta uno bueno, y uno nuevo sin revisar
    /// pesa más que el rechazado al que reemplaza.
    /// </summary>
    public static DocumentStatusEnum ChecklistStatus(
        DocumentTypeEnum type, IEnumerable<(DocumentTypeEnum Type, DocumentStatusEnum Status)> documents)
    {
        var statuses = documents.Where(d => d.Type == type).Select(d => d.Status).ToHashSet();

        return ChecklistPriority.FirstOrDefault(statuses.Contains, DocumentStatusEnum.FALTANTE);
    }

    private static readonly DocumentStatusEnum[] ChecklistPriority =
        [DocumentStatusEnum.CORRECTO, DocumentStatusEnum.RECIBIDO, DocumentStatusEnum.ERROR, DocumentStatusEnum.FALTANTE];
}