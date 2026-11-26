using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Core.Permissions;

/// <summary>Cómo se muestra un módulo en el menú lateral y en las tarjetas de accesos rápidos.</summary>
/// <param name="Controlador">Controller de MVC al que lleva (acción <c>Index</c>).</param>
public record ModuloViewModel(ModuleEnum Modulo, string Texto, string Controlador, string Descripcion);
