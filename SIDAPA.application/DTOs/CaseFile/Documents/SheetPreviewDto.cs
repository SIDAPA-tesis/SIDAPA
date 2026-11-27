namespace SIDAPA.application.DTOs.CaseFile.Documents;

public record SheetPreviewDto(string Name, IReadOnlyList<IReadOnlyList<string>> Rows);
