using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIDAPA.application.DTOs.Core.Auth;
using SIDAPA.application.Interface;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Security;

namespace SIDAPA.presentation.test;

/// <summary>
/// Levanta SIDAPA.presentation completo en memoria (pipeline real: JWT en cookie, políticas, filtros, MVC).
/// <list type="bullet">
/// <item>Entorno <c>Testing</c>: no carga user-secrets ni siembra usuarios de prueba.</item>
/// <item>Configuración mínima falsa (BD inalcanzable con timeout de 1 s, Jwt:Key de pruebas, service account
/// generado en memoria) solo para que <c>Program.cs</c> arranque.</item>
/// <item>Bucket, lector y correo se sustituyen por NSubstitute: nunca sale tráfico real.</item>
/// </list>
/// Compártela con <c>IClassFixture&lt;SidapaWebFactory&gt;</c>: arrancar el host cuesta.
/// </summary>
public sealed class SidapaWebFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "clave-solo-para-pruebas-de-sidapa-0123456789";

    public ICloudStorageService Storage { get; } = Substitute.For<ICloudStorageService>();
    public ITaxScanClient TaxScan { get; } = Substitute.For<ITaxScanClient>();
    public IEmailSender Email { get; } = Substitute.For<IEmailSender>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=127.0.0.1;Port=1;Database=sidapa_test;Username=test;Password=test;Timeout=1");
        builder.UseSetting("TaxScan:BaseUrl", "http://reader.test");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("GoogleCloudStorage:BucketName", "sidapa-test");
        builder.UseSetting("GoogleCloudStorage:CredentialsJson", FakeServiceAccountJson());
        builder.UseSetting("Database:MigrateOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICloudStorageService>();
            services.AddSingleton(Storage);
            services.RemoveAll<ITaxScanClient>();
            services.AddSingleton(TaxScan);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(Email);
        });
    }

    /// <summary>Cliente sin sesión. No sigue redirecciones para poder afirmar a dónde manda (login, denegado…).</summary>
    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")   // la cookie es Secure y hay UseHttpsRedirection
        });

    /// <summary>Cliente con la cookie <c>sidapa_token</c> de un usuario del rol indicado (token firmado por la app).</summary>
    public HttpClient CreateClientAs(RolesEnum role, int userId = 1, int? clientId = null)
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={CreateToken(role, userId, clientId).AccessToken}");
        return client;
    }

    public AuthToken CreateToken(RolesEnum role, int userId = 1, int? clientId = null)
    {
        var tokens = Services.GetRequiredService<ITokenService>();
        return tokens.CreateToken(new UserSessionDto(userId, $"Usuario {role}", $"{role}@sidapa.test".ToLowerInvariant(), role, clientId));
    }

    /// <summary>
    /// Service account con una llave RSA recién generada: solo sirve para que Program.cs construya el
    /// StorageClient/UrlSigner. Nunca se usa contra Google porque <see cref="ICloudStorageService"/> se sustituye.
    /// </summary>
    private static string FakeServiceAccountJson()
    {
        using var rsa = RSA.Create(2048);
        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["type"] = "service_account",
            ["project_id"] = "sidapa-test",
            ["private_key_id"] = "test",
            ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(),
            ["client_email"] = "test@sidapa-test.iam.gserviceaccount.com",
            ["client_id"] = "1",
            ["token_uri"] = "https://oauth2.googleapis.com/token"
        });
    }
}
