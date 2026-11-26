using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Core.Permissions;

/// <summary>Tarjetas de "Accesos rápidos" de la página de inicio: los módulos del rol, sin Inicio.</summary>
public record AccesosRapidosViewModel(IReadOnlyList<ModuloViewModel> Accesos)
{
    public static AccesosRapidosViewModel Desde(IEnumerable<ModuleEnum> modulos) =>
        new(ModuloCatalogo.Para(modulos.Where(m => m != ModuleEnum.Inicio)));
}
