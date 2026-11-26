namespace SIDAPA.presentation.Models.Core.Notifications;

/// <summary>Una tarjeta de novedades. <paramref name="Fecha"/> ya está en hora local.</summary>
public record NovedadViewModel(string Mensaje, DateTime Fecha, bool Nueva);
