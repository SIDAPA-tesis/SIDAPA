using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.application.Interface;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Interface.External.CloudStorage;
using SIDAPA.application.Services.Core;
using SIDAPA.infraestructure.ApiClients;
using SIDAPA.infraestructure.External.CloudStorage;
using SIDAPA.infraestructure.Email;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
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

        // Con servidor configurado manda correos reales; si no, solo los escribe en el log.
        services.AddSingleton(email);
        if (email.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        // Google Cloud Storage: "el contexto" (StorageClient + UrlSigner) se construye una sola vez
        // desde el service account y se registra como singleton; la app solo ve ICloudStorageService.
        var credential = CredentialFactory.FromJson<ServiceAccountCredential>(gcs.CredentialsJson).ToGoogleCredential();
        services.AddSingleton(gcs);
        services.AddSingleton(StorageClient.Create(credential));
        services.AddSingleton(UrlSigner.FromCredential(credential));
        services.AddSingleton<ICloudStorageService, GoogleCloudStorageService>();

        return services;
    }
}