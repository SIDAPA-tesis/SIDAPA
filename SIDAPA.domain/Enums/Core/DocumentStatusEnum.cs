namespace SIDAPA.domain.Enums.Core;

/// <summary>Estado de un documento del expediente (columna ope_documento.estado, se guarda el nombre).</summary>
public enum DocumentStatusEnum
{
    RECIBIDO = 1,
    CORRECTO = 2,
    FALTANTE = 3,
    ERROR = 4
}