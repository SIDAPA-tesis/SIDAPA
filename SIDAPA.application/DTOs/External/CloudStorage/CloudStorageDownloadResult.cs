namespace SIDAPA.application.DTOs.External.CloudStorage;

/// <summary>
/// Contenido descargado de un objeto. <see cref="Content"/> queda posicionado al inicio;
/// el consumidor es responsable de liberarlo (using/await using).
/// </summary>
public record CloudStorageDownloadResult(Stream Content, string ContentType, long SizeInBytes);
