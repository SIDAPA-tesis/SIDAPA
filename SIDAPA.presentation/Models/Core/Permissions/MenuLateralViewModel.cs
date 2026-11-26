using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Core.Permissions;

/// <summary>Menú lateral del dashboard: quién está en sesión y los módulos de su rol.</summary>
public record MenuLateralViewModel(string Nombre, string Rol, IReadOnlyList<OpcionMenuViewModel> Opciones)
{
    /// <param name="controladorActual">Controller de la petición; su opción sale marcada como activa.</param>
    public static MenuLateralViewModel Desde(
        string nombre, IEnumerable<string> roles, IEnumerable<ModuleEnum> modulos, string? controladorActual)
    {
        var opciones = ModuloCatalogo.Para(modulos)
            .Select(m => new OpcionMenuViewModel(
                m.Texto,
                m.Controlador,
                string.Equals(controladorActual, m.Controlador, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new MenuLateralViewModel(nombre, string.Join(", ", roles), opciones);
    }
}
