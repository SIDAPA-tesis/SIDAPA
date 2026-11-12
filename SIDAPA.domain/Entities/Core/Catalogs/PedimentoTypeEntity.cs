using SIDAPA.domain.Enums.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIDAPA.domain.Entities.Core.Catalogs
{
    public class PedimentoTypeEntity
    {
        public int Id { get; init; }
        public required string Numero { get; init; }
        public required string Empresa { get; init; }
        public string? ImagenUrl { get; init; }

        /// <summary>Porcentaje de avance, de 0 a 100.</summary>
        public int Avance { get; init; }

        public DateOnly FechaLimite { get; init; }

        public UrgenciaEnum UrgenciaEn(DateOnly hoy)
        {
            if (Avance >= 100) return UrgenciaEnum.Completado;
            if (FechaLimite < hoy) return UrgenciaEnum.Vencido;
            return FechaLimite <= hoy.AddDays(7) ? UrgenciaEnum.Proximo : UrgenciaEnum.EnTiempo;
        }
    }
}
