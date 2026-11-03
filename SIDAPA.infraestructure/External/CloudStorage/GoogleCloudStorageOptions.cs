namespace SIDAPA.infraestructure.External.CloudStorage;

/// <summary>
/// Configuración específica de Google Cloud Storage. POCO plano (sin IOptions&lt;T&gt;), enlazado en
/// Program.cs con GetSection("GoogleCloudStorage").Get&lt;T&gt;() y registrado como singleton — mismo
/// patrón que JwtOptions. <see cref="CredentialsJson"/> es secreto (user-secrets), no va en appsettings.
/// </summary>
public class GoogleCloudStorageOptions
{
    /// <summary>Nombre del bucket donde se guardan los objetos.</summary>
    public string BucketName { get; set; } = "";

    /// <summary>JSON completo del service account (viene de user-secrets / variable de entorno).</summary>
    public string CredentialsJson { get; set; } = "";

    /// <summary>Vigencia por defecto (en minutos) de las URLs firmadas.</summary>
    public int SignedUrlExpiryMinutes { get; set; } = 15;
}
