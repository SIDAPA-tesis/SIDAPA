using SIDAPA.application.DTOs.Core.Referencias;

namespace SIDAPA.presentation.Models.Dev;

public class DevExpedienteViewModel
{
    public int ReferenciaId { get; set; } = 1;
    public bool Apply { get; set; }
    public ExpedienteStatusDto? Result { get; set; }
}