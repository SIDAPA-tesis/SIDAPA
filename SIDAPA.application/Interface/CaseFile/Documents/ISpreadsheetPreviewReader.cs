using SIDAPA.application.DTOs.CaseFile.Documents;

namespace SIDAPA.application.Interface.CaseFile.Documents;

/// <summary>Convierte un XLS/XLSX en filas de texto para la vista previa (la librería vive en infraestructura).</summary>
public interface ISpreadsheetPreviewReader
{
    SpreadsheetPreviewDto Read(Stream content);
}
