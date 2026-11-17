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
}