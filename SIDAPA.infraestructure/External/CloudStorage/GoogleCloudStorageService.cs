using Google.Cloud.Storage.V1;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.External.CloudStorage;

namespace SIDAPA.infraestructure.External.CloudStorage;

/// <summary>
/// Implementación concreta de <see cref="ICloudStorageService"/> sobre Google Cloud Storage.
/// El <see cref="StorageClient"/> y el <see cref="UrlSigner"/> se construyen una sola vez (ver DI)
/// a partir del service account; aquí no se filtra ningún tipo de Google hacia la capa application.
/// </summary>
public class GoogleCloudStorageService(
    StorageClient storage, UrlSigner urlSigner, GoogleCloudStorageOptions options) : ICloudStorageService
{
    public async Task<CloudStorageObject> UploadAsync(CloudStorageUploadRequest request, CancellationToken ct = default)
    {
        var obj = await storage.UploadObjectAsync(
            options.BucketName, request.ObjectPath, request.ContentType, request.Content, cancellationToken: ct);

        return new CloudStorageObject(obj.Name, (long)(obj.Size ?? 0), obj.ContentType ?? request.ContentType);
    }

    public async Task<CloudStorageDownloadResult> DownloadAsync(string objectPath, CancellationToken ct = default)
    {
        var destination = new MemoryStream();
        var obj = await storage.DownloadObjectAsync(options.BucketName, objectPath, destination, cancellationToken: ct);
        destination.Position = 0;

        return new CloudStorageDownloadResult(destination, obj.ContentType ?? "application/octet-stream", (long)(obj.Size ?? 0));
    }

    public Task DeleteAsync(string objectPath, CancellationToken ct = default) =>
        storage.DeleteObjectAsync(options.BucketName, objectPath, cancellationToken: ct);

    public Task<string> GenerateSignedUrlAsync(string objectPath, TimeSpan? expiresIn = null, CancellationToken ct = default)
    {
        var duration = expiresIn ?? TimeSpan.FromMinutes(options.SignedUrlExpiryMinutes);
        return urlSigner.SignAsync(options.BucketName, objectPath, duration, HttpMethod.Get, cancellationToken: ct);
    }
}
