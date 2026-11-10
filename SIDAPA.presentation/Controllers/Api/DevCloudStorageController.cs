using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.External.CloudStorage;
using SIDAPA.application.Interface.External.CloudStorage;

namespace SIDAPA.presentation.Controllers.Api;

/// <summary>
/// Prueba de humo del almacenamiento (ICloudStorageService): sube → firma URL → descarga →
/// baja por la URL firmada → borra → confirma que ya no existe, verificando integridad con SHA-256.
/// Solo responde en Development. Endpoint de pruebas: no construir features sobre él.
/// </summary>
[ApiController]
[Route("api/dev/cloudstorage")]
public class DevCloudStorageController(
    IWebHostEnvironment env, ICloudStorageService storage, IHttpClientFactory httpFactory) : ControllerBase
{
    private const long RequestLimit = 50L * 1024 * 1024;
    private const int SignedUrlMinutes = 5;

    [HttpPost("roundtrip")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    public async Task<IActionResult> RoundTrip(IFormFile file, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Selecciona un archivo no vacío (campo 'file')." });

        // 1) Guarda los bytes originales (longitud + hash) para comparar al final.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var uploadedBytes = buffer.ToArray();
        var uploadedHash = Convert.ToHexString(SHA256.HashData(uploadedBytes));

        var objectPath = $"dev/roundtrip/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

        // 2) Subir.
        buffer.Position = 0;
        var stored = await storage.UploadAsync(new CloudStorageUploadRequest(buffer, objectPath, contentType), ct);

        // 3) URL firmada (antes de borrar, para que apunte a un objeto existente).
        var signedUrl = await storage.GenerateSignedUrlAsync(objectPath, TimeSpan.FromMinutes(SignedUrlMinutes), ct);

        // 4) Descargar por el servicio (SDK) y comparar bytes.
        var download = await storage.DownloadAsync(objectPath, ct);
        string downloadedHash;
        long downloadedLength;
        await using (download.Content)
        {
            using var downloadBuffer = new MemoryStream();
            await download.Content.CopyToAsync(downloadBuffer, ct);
            var downloadedBytes = downloadBuffer.ToArray();
            downloadedLength = downloadedBytes.Length;
            downloadedHash = Convert.ToHexString(SHA256.HashData(downloadedBytes));
        }
        var downloadMatches = downloadedHash == uploadedHash;

        // 5) Bajar por la URL firmada vía HTTP (única prueba real de que la firma funciona).
        string signedUrlStatus;
        var signedUrlMatches = false;
        try
        {
            var http = httpFactory.CreateClient();
            using var response = await http.GetAsync(signedUrl, ct);
            signedUrlStatus = $"{(int)response.StatusCode} {response.StatusCode}";
            if (response.IsSuccessStatusCode)
            {
                var signedBytes = await response.Content.ReadAsByteArrayAsync(ct);
                signedUrlMatches = Convert.ToHexString(SHA256.HashData(signedBytes)) == uploadedHash;
            }
        }
        catch (Exception ex)
        {
            signedUrlStatus = "error: " + ex.Message;
        }

        // 6) Borrar y confirmar que ya no existe (DownloadAsync debe fallar).
        await storage.DeleteAsync(objectPath, ct);
        bool confirmedGone;
        try
        {
            var afterDelete = await storage.DownloadAsync(objectPath, ct);
            await afterDelete.Content.DisposeAsync();
            confirmedGone = false;
        }
        catch
        {
            confirmedGone = true;
        }

        return Ok(new
        {
            ok = downloadMatches && signedUrlMatches && confirmedGone,
            objectPath,
            contentType,
            uploaded = new { length = uploadedBytes.Length, sha256 = uploadedHash, storedSizeInBytes = stored.SizeInBytes },
            download = new { length = downloadedLength, sha256 = downloadedHash, bytesMatch = downloadMatches },
            signedUrl = new { url = signedUrl, expiresInMinutes = SignedUrlMinutes, status = signedUrlStatus, bytesMatch = signedUrlMatches },
            delete = new { done = true, confirmedGone }
        });
    }
}
