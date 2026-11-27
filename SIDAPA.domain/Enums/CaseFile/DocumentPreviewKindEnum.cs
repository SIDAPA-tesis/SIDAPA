namespace SIDAPA.domain.Enums.CaseFile;

/// <summary>Cómo se muestra un documento en la vista previa sin descargarlo: Excel lo convierte el servidor en tabla.</summary>
public enum DocumentPreviewKindEnum
{
    None = 0,
    Pdf = 1,
    Text = 2,
    Spreadsheet = 3
}
