using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.External.CloudStorage;

namespace SIDAPA.application.test.Fakes;

/// <summary>
/// Bucket en memoria. Guarda los bytes completos de cada objeto para poder verificar que se subió
/// el archivo entero (y no solo lo que quedó después de que el validador leyó el encabezado).
/// </summary>
public sealed class FakeCloudStorage : ICloudStorageService
{
    public Dictionary<string, byte[]> Uploaded { get; } = [];
    public Dictionary<string, string> ContentTypes { get; } = [];
    public List<string> Deleted { get; } = [];
    public List<(string ObjectPath, TimeSpan? ExpiresIn)> SignedUrls { get; } = [];

    public async Task<CloudStorageObject> UploadAsync(CloudStorageUploadRequest request, CancellationToken ct = default)
    {
        using var copy = new MemoryStream();
        await request.Content.CopyToAsync(copy, ct);
        Uploaded[request.ObjectPath] = copy.ToArray();
        ContentTypes[request.ObjectPath] = request.ContentType;
        return new CloudStorageObject(request.ObjectPath, copy.Length, request.ContentType);
    }

    public Task<CloudStorageDownloadResult> DownloadAsync(string objectPath, CancellationToken ct = default) =>
        throw new NotSupportedException("Ningún servicio de application descarga del bucket todavía.");

    public Task DeleteAsync(string objectPath, CancellationToken ct = default)
    {
        Uploaded.Remove(objectPath);
        ContentTypes.Remove(objectPath);
        Deleted.Add(objectPath);
        return Task.CompletedTask;
    }

    public Task<string> GenerateSignedUrlAsync(string objectPath, TimeSpan? expiresIn = null, CancellationToken ct = default)
    {
        SignedUrls.Add((objectPath, expiresIn));
        return Task.FromResult($"https://storage.test/{objectPath}?signed=1");
    }
}
