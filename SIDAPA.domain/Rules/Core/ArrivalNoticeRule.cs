using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Rules.Core;

/// <summary>Reglas del aviso de arribo (HU-02). Los archivos se validan aparte con <see cref="DocumentFileRule"/>.</summary>
public static class ArrivalNoticeRule
{
    public const int MaxNotesLength = 500;

    /// <summary>Tipos de operación que el cliente puede elegir hoy.</summary>
    public static readonly IReadOnlyList<OperationTypeEnum> AllowedOperationTypes = [OperationTypeEnum.IMPORTACION];

    public static void EnsureValid(OperationTypeEnum operationType, DateOnly? arrivalDate, string? notes, int documentCount)
    {
        if (!AllowedOperationTypes.Contains(operationType)) throw new OperationTypeNotAllowedException();

        if (arrivalDate is null) throw new ArrivalDateRequiredException();

        if (notes is { Length: > MaxNotesLength }) throw new NotesTooLongException(MaxNotesLength);

        if (documentCount == 0) throw new NoDocumentsException();
    }
}
