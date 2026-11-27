namespace SIDAPA.application.DTOs.CaseFile.Documents;

/// <param name="Truncated">Se recortaron hojas, filas o columnas para no armar una página enorme.</param>
public record SpreadsheetPreviewDto(IReadOnlyList<SheetPreviewDto> Sheets, bool Truncated);
