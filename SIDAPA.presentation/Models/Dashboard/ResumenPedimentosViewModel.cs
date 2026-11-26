using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Dashboard;

/// <summary>
/// Resumen de pedimentos en Inicio para el personal (Capturador, Glosador, Administrador): cuántos hay por urgencia
/// y los que vencen primero. Sale del mismo servicio que el panel de Pedimentos.
/// </summary>
public record ResumenPedimentosViewModel(
    int Total,
    IReadOnlyList<IndicadorPedimentosViewModel> Indicadores,
    IReadOnlyList<PedimentoDto> ProximosAVencer)
{
    public const int MaximoProximos = 5;

    private static readonly (UrgenciaEnum Urgencia, string Titulo)[] Orden =
    [
        (UrgenciaEnum.Vencido, "Vencidos"),
        (UrgenciaEnum.Proximo, "Próximos a vencer"),
        (UrgenciaEnum.EnTiempo, "En tiempo"),
        (UrgenciaEnum.Completado, "Completados"),
    ];

    public static ResumenPedimentosViewModel Desde(IReadOnlyList<PedimentoDto> pedimentos) => new(
        pedimentos.Count,
        Orden.Select(o => new IndicadorPedimentosViewModel(o.Titulo, pedimentos.Count(p => p.Urgencia == o.Urgencia), o.Urgencia.ColorBootstrap()))
            .ToList(),
        // Los pendientes que vencen primero (los vencidos quedan arriba por tener la fecha más antigua).
        pedimentos.Where(p => p.Urgencia != UrgenciaEnum.Completado)
            .OrderBy(p => p.FechaLimite)
            .Take(MaximoProximos)
            .ToList());
}
