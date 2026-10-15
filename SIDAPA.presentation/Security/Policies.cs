namespace SIDAPA.presentation.Security;

/// <summary>Qué rol puede usar qué parte del sistema (ver docs/permisos-por-rol.md).</summary>
public static class Policies
{
    public const string ClientPortal = nameof(ClientPortal);     // Cliente: avisos, sus documentos, sus operaciones
    public const string Staff = nameof(Staff);                   // Personal interno: Capturador, Glosador, Administrador
    public const string Capture = nameof(Capture);               // Captura: Capturador, Administrador
    public const string Glosa = nameof(Glosa);                   // Glosa: Glosador, Administrador
    public const string Administration = nameof(Administration); // Administración: Administrador
}