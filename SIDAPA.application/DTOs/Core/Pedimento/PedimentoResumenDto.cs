namespace SIDAPA.application.DTOs.Core.Pedimento;

/// <summary>Lectura de una referencia para el panel de pedimentos (cards y calendario).</summary>
public record PedimentoResumenDto(
    int Id,
    string Numero,
    string Empresa,
    string? ImagenUrl,
    short StatusId,
    DateOnly FechaLimite);
