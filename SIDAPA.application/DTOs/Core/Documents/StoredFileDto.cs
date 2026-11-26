namespace SIDAPA.application.DTOs.Core.Documents;

/// <summary>Archivo ya validado y subido al bucket, todavía sin fila en ope_documento.</summary>
/// <param name="UploadedAt">UTC sin zona (la columna es "timestamp without time zone").</param>
public record StoredFileDto(string BucketPath, string FileName, long SizeInBytes, DateTime UploadedAt);
