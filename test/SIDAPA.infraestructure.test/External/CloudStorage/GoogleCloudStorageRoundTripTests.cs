using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.infraestructure.External.CloudStorage;

namespace SIDAPA.infraestructure.test.External.CloudStorage;

/// <summary>
/// Prueba de humo contra un bucket REAL de Google Cloud Storage: sube → firma URL → baja por HTTP → descarga
/// por SDK → borra → confirma que ya no existe, comparando SHA-256. Reemplaza a DevCloudStorageController.
/// <para>
/// Se omite salvo que existan <c>SIDAPA_GCS_TEST_BUCKET</c> (bucket de pruebas, nunca el de producción) y
/// <c>GOOGLE_APPLICATION_CREDENTIALS</c> (ruta al JSON del service account). Ejemplo:
/// <c>$env:SIDAPA_GCS_TEST_BUCKET="sidapa-dev"; $env:GOOGLE_APPLICATION_CREDENTIALS="C:\ruta\sa.json"; dotnet test --project test/SIDAPA.infraestructure.test --filter-class "*RoundTripTests"</c>
/// </para>
/// </summary>
public class GoogleCloudStorageRoundTripTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RoundTrip_SubeFirmaDescargaYBorra_SinPerderBytes()
    {
        var bucket = Environment.GetEnvironmentVariable("SIDAPA_GCS_TEST_BUCKET");
        var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(credentialsPath),
            "Define SIDAPA_GCS_TEST_BUCKET y GOOGLE_APPLICATION_CREDENTIALS para correr la prueba contra GCS real.");

        var credential = CredentialFactory
            .FromJson<ServiceAccountCredential>(await File.ReadAllTextAsync(credentialsPath!, _ct))
            .ToGoogleCredential();
        var service = new GoogleCloudStorageService(
            await StorageClient.CreateAsync(credential),
            UrlSigner.FromCredential(credential),
            new GoogleCloudStorageOptions { BucketName = bucket!, CredentialsJson = "{}", SignedUrlExpiryMinutes = 5 });

        var bytes = RandomNumberGenerator.GetBytes(64 * 1024);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var path = $"tests/roundtrip/{Guid.NewGuid():N}.bin";

        try
        {
            var stored = await service.UploadAsync(new CloudStorageUploadRequest(new MemoryStream(bytes), path, "application/octet-stream"), _ct);
            stored.SizeInBytes.ShouldBe(bytes.Length);

            // La URL firmada es la única prueba real de que la firma funciona.
            var url = await service.GenerateSignedUrlAsync(path, TimeSpan.FromMinutes(5), _ct);
            using var http = new HttpClient();
            var viaUrl = await http.GetByteArrayAsync(url, _ct);
            Convert.ToHexString(SHA256.HashData(viaUrl)).ShouldBe(hash);

            var download = await service.DownloadAsync(path, _ct);
            await using (download.Content)
            {
                using var copy = new MemoryStream();
                await download.Content.CopyToAsync(copy, _ct);
                Convert.ToHexString(SHA256.HashData(copy.ToArray())).ShouldBe(hash);
            }
        }
        finally
        {
            await service.DeleteAsync(path, CancellationToken.None);
        }

        await Should.ThrowAsync<Exception>(() => service.DownloadAsync(path, _ct));
    }
}
