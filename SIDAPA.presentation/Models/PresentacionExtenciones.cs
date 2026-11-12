using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models
{
    public static class PresentacionExtenciones
    {
        public static string ClaseEstado(this UrgenciaEnum u) => u switch
        {
            UrgenciaEnum.Vencido => "error",
            UrgenciaEnum.Proximo => "aviso",
            UrgenciaEnum.EnTiempo => "ok",
            _ => "neutro"
        };

        public static string Etiqueta(this UrgenciaEnum u) => u switch
        {
            UrgenciaEnum.Vencido => "Vencido",
            UrgenciaEnum.Proximo => "Proximo",
            UrgenciaEnum.EnTiempo => "En Tiempo",
            _ => "Completado",
        };

        public static string ClaseEstado(this ClaveStateEnum e) => e switch
        {
            ClaveStateEnum.Activo => "ok",
            ClaveStateEnum.PorVencer => "aviso",
            _ => "error"
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
