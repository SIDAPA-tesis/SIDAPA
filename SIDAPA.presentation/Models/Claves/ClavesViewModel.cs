using Microsoft.AspNetCore.Mvc;
using SIDAPA.application.DTOs.Core.Claves;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Claves
{
    public record ClavesViewModel(TableroClavesDto Tablero, string? Busqueda, bool? Vigente)
    {
        public Dictionary<string, string> Ruta(string? clave = null)
        {
            var ruta = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(Busqueda)) ruta["q"] = Busqueda;
            if (Vigente is { } v) ruta["vigente"] = v ? "true" : "false";
            if (clave is not null) ruta["clave"] = clave;
            return ruta;
        }
    }

}
