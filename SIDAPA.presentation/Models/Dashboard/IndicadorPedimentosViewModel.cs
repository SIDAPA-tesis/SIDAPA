namespace SIDAPA.presentation.Models.Dashboard;

/// <summary>Una tarjeta de conteo en Inicio ("Vencidos: 3"). <see cref="Color"/> es un color contextual de Bootstrap.</summary>
public record IndicadorPedimentosViewModel(string Titulo, int Total, string Color);
