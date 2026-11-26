using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Core.Permissions;

/// <summary>
/// Texto, controller y descripción de cada <see cref="ModuleEnum"/>. Qué módulos ve cada rol lo decide
/// <c>IPermisosService</c> (application); aquí solo vive cómo se pintan. Un módulo nuevo se agrega aquí una vez
/// y aparece solo en el menú lateral y en los accesos rápidos de los roles que lo tengan.
/// </summary>
public static class ModuloCatalogo
{
    private static readonly Dictionary<ModuleEnum, ModuloViewModel> Modulos = new ModuloViewModel[]
    {
        new(ModuleEnum.Inicio, "Inicio", "Home", "Resumen de tu actividad en SIDAPA."),
        new(ModuleEnum.Pedimentos, "Pedimentos", "Pedimentos", "Consulta los pedimentos y sus fechas de pago."),
        new(ModuleEnum.Claves, "Claves", "Claves", "Catálogo de claves del Anexo 22 y su vigencia."),
        new(ModuleEnum.Reportes, "Reportes", "Reportes", "Revisa las altas por mes."),
        new(ModuleEnum.Productos, "Productos", "Productos", "Productos de los clientes y sus fracciones."),
        new(ModuleEnum.Usuarios, "Usuarios", "Usuarios", "Consulta las cuentas y sus roles."),
        new(ModuleEnum.AvisosArribo, "Mis avisos de arribo", "Avisos", "Envía tus documentos y sigue tus referencias."),
    }.ToDictionary(m => m.Modulo);

    /// <summary>Los módulos indicados, en el mismo orden.</summary>
    public static IReadOnlyList<ModuloViewModel> Para(IEnumerable<ModuleEnum> modulos) =>
        modulos.Select(Get).ToList();

    public static ModuloViewModel Get(ModuleEnum modulo) =>
        Modulos.TryGetValue(modulo, out var vm)
            ? vm
            : throw new InvalidOperationException($"El módulo {modulo} no está en ModuloCatalogo.");
}
