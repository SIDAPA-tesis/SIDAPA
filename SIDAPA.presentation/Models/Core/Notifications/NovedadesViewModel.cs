using SIDAPA.application.DTOs.Core.Notifications;

namespace SIDAPA.presentation.Models.Core.Notifications;

/// <summary>Sección "Novedades" de la página de inicio: los avisos recientes del usuario.</summary>
public record NovedadesViewModel(IReadOnlyList<NovedadViewModel> Novedades)
{
    public static readonly NovedadesViewModel Vacio = new([]);

    public static NovedadesViewModel Desde(IReadOnlyList<NotificationDto> avisos, TimeZoneInfo zonaHoraria) =>
        new(avisos
            .Select(a => new NovedadViewModel(a.Message, HoraLocal(a.CreatedAt, zonaHoraria), Nueva: !a.IsRead))
            .ToList());

    // La base guarda UTC sin zona.
    private static DateTime HoraLocal(DateTime utc, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zona);
}
