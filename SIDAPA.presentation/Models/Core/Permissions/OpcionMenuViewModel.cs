namespace SIDAPA.presentation.Models.Core.Permissions;

/// <summary>Una liga del menú lateral. <paramref name="Activa"/> marca la pantalla actual.</summary>
public record OpcionMenuViewModel(string Texto, string Controlador, bool Activa);
