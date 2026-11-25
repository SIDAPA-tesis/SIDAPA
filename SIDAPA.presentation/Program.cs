using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SIDAPA.application;
using SIDAPA.application.Interface.Core;
using SIDAPA.infraestructure;
using SIDAPA.infraestructure.External.CloudStorage;
using SIDAPA.infraestructure.Email;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Persistence.Seed;
using SIDAPA.infraestructure.Security;
using SIDAPA.presentation.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. MVC with classic .cshtml Razor views (no Blazor / Razor Components).
builder.Services.AddControllersWithViews();

// Database (PostgreSQL + EF Core). The connection string is NOT stored in appsettings.json:
// set it with `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>"`.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'ConnectionStrings:DefaultConnection'. Configure it with dotnet user-secrets.");
}

// Document reader (sidapa-reader, Python/FastAPI). Local: http://localhost:8000 · Docker: http://reader:8000
var readerBaseUrl = builder.Configuration["TaxScan:BaseUrl"];
if (string.IsNullOrWhiteSpace(readerBaseUrl))
{
    throw new InvalidOperationException("Missing 'TaxScan:BaseUrl' in configuration.");
}

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Missing or short 'Jwt:Key' (min 32 chars). Set it with dotnet user-secrets.");

// Google Cloud Storage. BucketName/SignedUrlExpiryMinutes en appsettings; CredentialsJson (JSON
// completo del service account) en user-secrets: dotnet user-secrets set "GoogleCloudStorage:CredentialsJson" "<json>".
var gcs = builder.Configuration.GetSection("GoogleCloudStorage").Get<GoogleCloudStorageOptions>()
          ?? new GoogleCloudStorageOptions();
if (string.IsNullOrWhiteSpace(gcs.BucketName))
    throw new InvalidOperationException("Missing 'GoogleCloudStorage:BucketName' in configuration.");
if (string.IsNullOrWhiteSpace(gcs.CredentialsJson))
    throw new InvalidOperationException("Missing 'GoogleCloudStorage:CredentialsJson'. Set it with dotnet user-secrets.");
var email = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
builder.Services.AddApplication();
builder.Services.AddInfraestructure(connectionString, readerBaseUrl, jwt, gcs, email);

// Usuario de la petición actual (menú lateral por rol).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserActual, UsuarioActual>();

builder.Services.AddSidapaAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // conserva los nombres cortos: sub, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"          // para que [Authorize(Roles = nameof(RolesEnum.GLOSADOR))] funcione
        };
        options.Events = new JwtBearerEvents
        {
            // Pantallas sin sesión: al login (las APIs se quedan con 401).
            OnChallenge = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api")) return Task.CompletedTask;
                ctx.HandleResponse();
                var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
                ctx.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
                return Task.CompletedTask;
            },
            // Con sesión pero sin permiso: página de acceso denegado (las APIs se quedan con 403).
            OnForbidden = ctx =>
            {
                if (!ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.Redirect("/Auth/Denied");
                return Task.CompletedTask;
            },
            // el token viene en la cookie, no en el encabezado Authorization
            OnMessageReceived = ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Request.Headers.Authorization))
                    ctx.Token = ctx.Request.Cookies[AuthCookie.Name];
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Migraciones automáticas al arrancar. Apagado por defecto (en local se sigue usando `dotnet ef database update`);
// docker-compose lo enciende con Database__MigrateOnStartup=true. Si falla, la app NO arranca:
// es preferible un contenedor que se reinicia con el error en el log a una app sobre un esquema incompleto.
var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
if (migrateOnStartup)
    await DatabaseMigrator.MigrateAsync(app.Services);

// Roles (cat_rol) y, solo en Development, usuarios de prueba <rol>@sidapa.local.
// La contraseña de prueba sale de user-secrets (DevUsers:Password) o, en Docker, de DEV_USERS_PASSWORD en .env.
var devUsersPassword = app.Environment.IsDevelopment() ? app.Configuration["DevUsers:Password"] : null;
if (migrateOnStartup)
{
    // Docker: la base la acaba de preparar el migrador, así que un fallo aquí es un error real y debe tumbar el arranque.
    await DatabaseSeeder.SeedAsync(app.Services, devUsersPassword);
}
else
{
    // Local: se deja arrancar la app aunque la base no esté lista, pero con el error completo en el log.
    try
    {
        await DatabaseSeeder.SeedAsync(app.Services, devUsersPassword);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "No se pudieron crear los datos iniciales (roles y usuarios de prueba). Revisa la excepción; " +
            "si la base está vacía corre `dotnet ef database update` o arranca con Database:MigrateOnStartup=true.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Index/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<TokenRenewalMiddleware>();   // renueva el token a la mitad de su vida
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Index}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();