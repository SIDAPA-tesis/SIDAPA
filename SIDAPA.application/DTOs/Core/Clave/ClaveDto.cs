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
        DateOnly? FechaActualizacion);


    public record TableroClavesDto(
        MetricasClavesDto Metricas,
        IReadOnlyList<ClaveDto> Claves,
        ClaveDto? Seleccionada);
}
