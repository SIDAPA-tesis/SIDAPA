using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models
{
    public static class PresentacionExtenciones
    {
        /// <summary>Color contextual de Bootstrap (danger, warning…) para badges y fondos de la urgencia.</summary>
        public static string ColorBootstrap(this UrgenciaEnum u) => u switch
        {
            UrgenciaEnum.Vencido => "danger",
            UrgenciaEnum.Proximo => "warning",
            UrgenciaEnum.EnTiempo => "success",
            _ => "secondary"
        };

        public static string Etiqueta(this UrgenciaEnum u) => u switch
        {
            UrgenciaEnum.Vencido => "Vencido",
            UrgenciaEnum.Proximo => "Próximo",
            UrgenciaEnum.EnTiempo => "En tiempo",
            _ => "Completado",
        };

        public static string Etiqueta(this ClaveStateEnum e) => e switch
        {
            ClaveStateEnum.Activo => "Activo",
            ClaveStateEnum.PorVencer => "Por vencer",
            _ => "Inactivo"
        };

        public static string Etiqueta(this ClaveTypeEnum t) => t switch
        {
            ClaveTypeEnum.FraccionArancelaria => "Fracción arancelaria",
            ClaveTypeEnum.ClavePedimento => "Clave de pedimento",
            _ => "Patente aduanal"
        };

        public static string Texto(this DateOnly f) => f.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        public static string TextoCorto(this DateOnly f) => f.ToString("dd/MM", CultureInfo.InvariantCulture);
        public static string Iso(this DateOnly f) => f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
