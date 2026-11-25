using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.application.Interface;
using SIDAPA.application.Interface.CaseFile.Documents;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.Core.Persistence;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.application.Services.Core;
using SIDAPA.infraestructure.ApiClients;
using SIDAPA.infraestructure.External.CloudStorage;
using SIDAPA.infraestructure.Email;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.Repositories.CaseFile.Documents;
using SIDAPA.infraestructure.Security;

namespace SIDAPA.infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructure(
        this IServiceCollection services, string connectionString, string readerBaseUrl, JwtOptions jwt,
        GoogleCloudStorageOptions gcs, EmailOptions email)
    {
        services.AddDbContext<SidapaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SidapaDbContext.Schema)));

        services.AddHttpClient<ITaxScanClient, TaxScanClient>(client =>
        {
            client.BaseAddress = new Uri(readerBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IDocumentFileValidator, DocumentFileValidator>();   // SCRUM-50

        // SCRUM-59: avisos (panel y correo)
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        // SCRUM-48: documentos del expediente (bucket + ope_documento)
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentService, DocumentService>();

        // SCRUM-56: expediente completo (cambio automático de estatus)
        services.AddScoped<IReferenciaRepository, ReferenciaRepository>();
        services.AddScoped<IExpedienteService, ExpedienteService>();

        // SCRUM-64 (HU-05): solicitudes de documentos y transacción entre repositorios
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDocumentRequestRepository, DocumentRequestRepository>();

        // Panel de pedimentos y catálogo de claves (servicios en AddApplication)
        services.AddScoped<IPedimentoRepositorio, PedimentoRepository>();
        services.AddScoped<IClaveRepositorio, ClaveRepository>();

        // SCRUM-46: catálogos del aviso de arribo (el servicio se registra en AddApplication)
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Con servidor configurado manda correos reales; si no, solo los escribe en el log.
        services.AddSingleton(email);
        if (email.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        // Google Cloud Storage: "el contexto" (StorageClient + UrlSigner) se construye una sola vez
        // desde el service account y se registra como singleton; la app solo ve ICloudStorageService.
        // El secreto puede venir como JSON crudo o como JSON en Base64 (más cómodo para env vars/Docker).
        var credentialsJson = ResolveCredentialsJson(gcs.CredentialsJson);
        var credential = CredentialFactory.FromJson<ServiceAccountCredential>(credentialsJson).ToGoogleCredential();
        services.AddSingleton(gcs);
        services.AddSingleton(StorageClient.Create(credential));
        services.AddSingleton(UrlSigner.FromCredential(credential));
        services.AddSingleton<ICloudStorageService, GoogleCloudStorageService>();

        return services;
    }

    /// <summary>
    /// Acepta el service account como JSON crudo (empieza con '{') o como JSON codificado en Base64,
    /// y devuelve siempre el JSON crudo listo para <see cref="CredentialFactory"/>.
    /// </summary>
    private static string ResolveCredentialsJson(string value)
    {
        var trimmed = value.Trim();

        // JSON crudo: ya está listo.
        if (trimmed.StartsWith('{'))
            return trimmed;

        // Si no, se asume Base64 (p. ej. para pasarlo por variable de entorno / Docker).
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(trimmed));
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "'GoogleCloudStorage:CredentialsJson' no es JSON válido ni Base64. " +
                "Debe ser el JSON del service account (crudo o en Base64).");
        }
    }
}