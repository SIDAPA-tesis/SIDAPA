namespace SIDAPA.presentation.Models.CaseFile.Documents;

/// <summary>Documentos pendientes de una misma referencia.</summary>
public record GrupoSolicitudesViewModel(string ReferenciaNumber, IReadOnlyList<SolicitudPendienteViewModel> Solicitudes)
{
    public string Faltan => Solicitudes.Count == 1 ? "Falta 1 documento" : $"Faltan {Solicitudes.Count} documentos";
}
