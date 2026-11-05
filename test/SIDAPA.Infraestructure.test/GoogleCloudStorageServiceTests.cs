using Google.Cloud.Storage.V1;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.infraestructure.External.CloudStorage;
using Xunit;
using StorageObject = Google.Apis.Storage.v1.Data.Object;

namespace SIDAPA.Infraestructure.test;

/// <summary>
/// Pruebas unitarias de <see cref="GoogleCloudStorageService"/>. Usan fakes a mano:
/// <see cref="FakeStorageClient"/> (StorageClient es abstracto con métodos virtuales) y
/// <see cref="FakeBlobSigner"/> (UrlSigner es sealed, pero acepta un IBlobSigner propio).
/// No tocan la red ni requieren credenciales reales.
/// </summary>
public class GoogleCloudStorageServiceTests
{
    private const string Bucket = "sidapa-test-bucket";

    private static GoogleCloudStorageService CreateService(
        FakeStorageClient client, int signedUrlExpiryMinutes = 15)
    {
        var signer = UrlSigner.FromBlobSigner(new FakeBlobSigner());
        var options = new GoogleCloudStorageOptions
        {
            BucketName = Bucket,
            CredentialsJson = "{}",
            SignedUrlExpiryMinutes = signedUrlExpiryMinutes
        };
        return new GoogleCloudStorageService(client, signer, options);
    }

    [Fact]
    public async Task UploadAsync_PassesBucketAndRequest_AndMapsResult()
    {
        var client = new FakeStorageClient(returnedContentType: "application/pdf");
        var service = CreateService(client);
        var content = new MemoryStream([1, 2, 3, 4, 5]);

        var result = await service.UploadAsync(
            new CloudStorageUploadRequest(content, "ref/42/factura.pdf", "application/pdf"), CancellationToken.None);

        Assert.Equal(Bucket, client.LastBucket);
        Assert.Equal("ref/42/factura.pdf", client.LastObjectName);
        Assert.Equal("application/pdf", client.LastContentType);
        Assert.Equal("ref/42/factura.pdf", result.ObjectPath);
        Assert.Equal(5, result.SizeInBytes);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public async Task UploadAsync_WhenReturnedContentTypeIsNull_FallsBackToRequestContentType()
    {
        var client = new FakeStorageClient(returnedContentType: null);
        var service = CreateService(client);
        var content = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAsync(
            new CloudStorageUploadRequest(content, "ref/1/a.xml", "application/xml"), CancellationToken.None);

        Assert.Equal("application/xml", result.ContentType);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsBufferedStreamAtPositionZero_WithMetadata()
    {
        var payload = new byte[] { 10, 20, 30, 40 };
        var client = new FakeStorageClient(downloadBytes: payload, returnedContentType: "application/pdf");
        var service = CreateService(client);

        var result = await service.DownloadAsync("ref/7/bl.pdf", CancellationToken.None);

        await using var stream = result.Content;
        Assert.Equal(0, stream.Position);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(payload.Length, result.SizeInBytes);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(payload, ms.ToArray());
        Assert.Equal(Bucket, client.LastBucket);
        Assert.Equal("ref/7/bl.pdf", client.LastObjectName);
    }

    [Fact]
    public async Task DownloadAsync_WhenReturnedContentTypeIsNull_FallsBackToOctetStream()
    {
        var client = new FakeStorageClient(downloadBytes: [1], returnedContentType: null);
        var service = CreateService(client);

        var result = await service.DownloadAsync("ref/1/x.bin", CancellationToken.None);

        await using var _ = result.Content;
        Assert.Equal("application/octet-stream", result.ContentType);
    }

    [Fact]
    public async Task DeleteAsync_CallsClientWithBucketAndPath()
    {
        var client = new FakeStorageClient();
        var service = CreateService(client);

        await service.DeleteAsync("ref/9/packing.xlsx", CancellationToken.None);

        Assert.Equal(1, client.DeleteCount);
        Assert.Equal(Bucket, client.DeletedBucket);
        Assert.Equal("ref/9/packing.xlsx", client.DeletedObject);
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithoutExpiry_UsesOptionsDefault()
    {
        var service = CreateService(new FakeStorageClient(), signedUrlExpiryMinutes: 15);

        var url = await service.GenerateSignedUrlAsync("ref/3/factura.pdf", ct: CancellationToken.None);

        Assert.Contains(Bucket, url);
        Assert.Contains("factura.pdf", url);
        Assert.Contains("X-Goog-Expires=900", url); // 15 min * 60
    }

    [Fact]
    public async Task GenerateSignedUrlAsync_WithExplicitExpiry_UsesProvidedValue()
    {
        var service = CreateService(new FakeStorageClient(), signedUrlExpiryMinutes: 15);

        var url = await service.GenerateSignedUrlAsync(
            "ref/3/factura.pdf", TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Contains("X-Goog-Expires=60", url);
    }
}

/// <summary>
/// Fake de <see cref="StorageClient"/>: captura los argumentos y simula el objeto devuelto.
/// StorageClient es abstracto pero sus métodos son virtuales (no abstractos), así que solo se
/// sobrescriben los tres que usa el servicio.
/// </summary>
internal sealed class FakeStorageClient(byte[]? downloadBytes = null, string? returnedContentType = "application/pdf")
    : StorageClient
{
    public string? LastBucket { get; private set; }
    public string? LastObjectName { get; private set; }
    public string? LastContentType { get; private set; }
    public int DeleteCount { get; private set; }
    public string? DeletedBucket { get; private set; }
    public string? DeletedObject { get; private set; }

    public override Task<StorageObject> UploadObjectAsync(
        string bucket, string objectName, string? contentType, Stream source,
        UploadObjectOptions? options, CancellationToken cancellationToken,
        IProgress<Google.Apis.Upload.IUploadProgress>? progress)
    {
        LastBucket = bucket;
        LastObjectName = objectName;
        LastContentType = contentType;

        using var ms = new MemoryStream();
        source.CopyTo(ms);

        return Task.FromResult(new StorageObject
        {
            Name = objectName,
            Size = (ulong)ms.Length,
            ContentType = returnedContentType
        });
    }

    public override Task<StorageObject> DownloadObjectAsync(
        string bucket, string objectName, Stream destination,
        DownloadObjectOptions? options, CancellationToken cancellationToken,
        IProgress<Google.Apis.Download.IDownloadProgress>? progress)
    {
        LastBucket = bucket;
        LastObjectName = objectName;

        var bytes = downloadBytes ?? [];
        destination.Write(bytes, 0, bytes.Length);

        return Task.FromResult(new StorageObject
        {
            Name = objectName,
            Size = (ulong)bytes.Length,
            ContentType = returnedContentType
        });
    }

    public override Task DeleteObjectAsync(
        string bucket, string objectName, DeleteObjectOptions? options, CancellationToken cancellationToken)
    {
        DeleteCount++;
        DeletedBucket = bucket;
        DeletedObject = objectName;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Fake de firmador: permite construir un <see cref="UrlSigner"/> real sin credenciales, para
/// verificar que GenerateSignedUrlAsync arma la URL con el bucket, el objeto y la expiración.
/// </summary>
internal sealed class FakeBlobSigner : UrlSigner.IBlobSigner
{
    public string Id => "test-sa@sidapa.iam.gserviceaccount.com";
    public string Algorithm => "GOOG4-RSA-SHA256";

    public string CreateSignature(byte[] data, UrlSigner.BlobSignerParameters parameters) => "abcdef1234567890";

    public Task<string> CreateSignatureAsync(
        byte[] data, UrlSigner.BlobSignerParameters parameters, CancellationToken cancellationToken) =>
        Task.FromResult("abcdef1234567890");
}
