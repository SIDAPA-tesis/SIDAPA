namespace SIDAPA.application.DTOs.External.CloudStorage;

/// <summary>
/// Datos para subir un objeto al almacenamiento. <see cref="ObjectPath"/> es la ruta/llave
/// dentro del bucket (p. ej. "referencias/{id}/factura.pdf").
/// </summary>
public record CloudStorageUploadRequest(Stream Content, string ObjectPath, string ContentType);
