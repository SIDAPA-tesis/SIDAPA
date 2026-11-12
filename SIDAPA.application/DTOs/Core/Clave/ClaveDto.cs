using System;
using System.Collections.Generic;
using SIDAPA.domain.Enums;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.application.DTOs.Core.Claves
{
    public record FiltroClaves(string? Busqueda, bool? Vigente);

    public record MetricasClavesDto(int Vigentes, int NoVigentes);

    public record ClaveDto(
        string Codigo,
        string Descripcion,
        string Grupo,
        string? SubGrupo,
        string? Supuestos,
        bool Vigente,
        DateTime FechaActualizacion);


    public record TableroClavesDto(
        MetricasClavesDto Metricas,
        IReadOnlyList<ClaveDto> Claves,
        ClaveDto? Seleccionada);
}