using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.CaseFile.Expediente;

/// <param name="VistaPreviaAbierta">Documento abierto en la vista previa, para que el POST la conserve.</param>
public record DocumentoRecibidoViewModel(
    int ReferenciaId,
    int Id,
    string Tipo,
    string Archivo,
    string Tamano,
    DocumentStatusEnum Estado,
    string SubidoPor,
    DateTime Subido,
    bool Seleccionado,
    bool PuedeMarcar,
    int? VistaPreviaAbierta)
{
    public static readonly DocumentStatusEnum[] Marcas =
        [DocumentStatusEnum.CORRECTO, DocumentStatusEnum.FALTANTE, DocumentStatusEnum.ERROR];
}
