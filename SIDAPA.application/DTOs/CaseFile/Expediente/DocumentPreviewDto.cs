using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.domain.Enums.CaseFile;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

/// <param name="Spreadsheet">Solo para Excel; PDF y XML los pinta el navegador desde <c>OpenFileAsync</c>.</param>
public record DocumentPreviewDto(int DocumentId, int ReferenciaId, string FileName, DocumentPreviewKindEnum Kind, SpreadsheetPreviewDto? Spreadsheet);
