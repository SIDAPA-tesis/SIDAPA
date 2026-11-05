using SIDAPA.application.DTOs.External.CloudStorage;

namespace SIDAPA.application.Interface.External.CloudStorage;

/// <summary>
/// Abstracción de almacenamiento de objetos, agnóstica de proveedor. La implementación concreta
/// (Google Cloud Storage, S3, Azure Blob…) vive en infraestructure; aquí solo circulan Stream,
/// string, TimeSpan y DTOs propios. <paramref name="objectPath"/> es la ruta/llave del objeto
/// dentro del bucket (la misma que se guarda en DocumentEntity.BucketPath).
/// </summary>
public interface ICloudStorageService
{
    /// <summary>Sube un objeto y devuelve su referencia (ruta, tamaño y content-type).</summary>
    Task<CloudStorageObject> UploadAsync(CloudStorageUploadRequest request, CancellationToken ct = default);

    /// <summary>Descarga un objeto; el Stream resultante queda posicionado al inicio.</summary>
    Task<CloudStorageDownloadResult> DownloadAsync(string objectPath, CancellationToken ct = default);

    /// <summary>Elimina un objeto del bucket.</summary>
    Task DeleteAsync(string objectPath, CancellationToken ct = default);

    /// <summary>
    /// Genera una URL firmada temporal de descarga (GET) sin exponer el bucket. Si no se indica
    /// <paramref name="expiresIn"/> se usa el valor por defecto de la configuración.
    /// </summary>
    Task<string> GenerateSignedUrlAsync(string objectPath, TimeSpan? expiresIn = null, CancellationToken ct = default);
}
