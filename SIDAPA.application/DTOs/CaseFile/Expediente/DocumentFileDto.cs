using SIDAPA.domain.Enums.CaseFile;

namespace SIDAPA.application.DTOs.CaseFile.Expediente;

public record DocumentFileDto(Stream Content, DocumentPreviewKindEnum Kind, string FileName);
