using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIDAPA.application.Interface;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.infraestructure.ApiClients;
using SIDAPA.infraestructure.Email;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.Security;

namespace SIDAPA.infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructure(
        this IServiceCollection services, string connectionString, string readerBaseUrl, JwtOptions jwt, EmailOptions email)
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
        //services.AddSingleton<IDocumentFileValidator, DocumentFileValidator>();   // SCRUM-50 (ya estaba en dev)

        // SCRUM-59: avisos (panel y correo)
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        // Con servidor configurado manda correos reales; si no, solo los escribe en el log.
        services.AddSingleton(email);
        if (email.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        return services;
    }
}