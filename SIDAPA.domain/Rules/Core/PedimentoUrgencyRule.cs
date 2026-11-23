using SIDAPA.domain.Enums.Core;

namespace SIDAPA.domain.Rules.Core;

/// <summary>Qué tan urgente es una referencia según su avance y su fecha límite.</summary>
public static class PedimentoUrgencyRule
{
    /// <summary>Días antes de la fecha límite en que la referencia pasa a "Próximo".</summary>
    public const int DaysBeforeDue = 7;

    public static UrgenciaEnum For(int avance, DateOnly fechaLimite, DateOnly hoy)
    {
        if (avance >= 100) return UrgenciaEnum.Completado;
        if (fechaLimite < hoy) return UrgenciaEnum.Vencido;
        return fechaLimite <= hoy.AddDays(DaysBeforeDue) ? UrgenciaEnum.Proximo : UrgenciaEnum.EnTiempo;
    }

    /// <summary>Avance en porcentaje según el estatus (RECIBIDA = 0 %, CERRADO = 100 %).</summary>
    public static int ProgressFor(ReferenciaStatusEnum status)
    {
        const int first = (int)ReferenciaStatusEnum.RECIBIDA;
        const int last = (int)ReferenciaStatusEnum.CERRADO;
        return ((int)status - first) * 100 / (last - first);
    }
}
