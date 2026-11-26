using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.CaseFile.Documents;

namespace SIDAPA.presentation.test.Models.CaseFile.Documents;

/// <summary>
/// ViewModels del aviso de documento faltante (SCRUM-64 · HU-05): la tarjeta agrupa por referencia y dice cuántos
/// faltan, las horas se muestran en hora local y el cliente recibe una confirmación clara después de subir.
/// </summary>
public class SolicitudesViewModelTests
{
    private static readonly TimeZoneInfo Mexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
    private static readonly DateTime RequestedUtc = new(2026, 11, 24, 18, 0, 0);

    private static PendingDocumentRequestDto Pendiente(int id, string referencia, string documento, string? nota = null) =>
        new(id, ReferenciaId: id, referencia, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, documento, nota, RequestedUtc);

    [Fact]
    public void Desde_VariasSolicitudes_LasAgrupaPorReferenciaYCuentaLasQueFaltan()
    {
        var vm = SolicitudesPendientesViewModel.Desde(
            [Pendiente(1, "R-000001", "B/L"), Pendiente(2, "R-000001", "Lista de empaque"), Pendiente(3, "R-000002", "Factura")],
            recibido: null, Mexico);

        vm.Grupos.Select(g => (g.ReferenciaNumber, g.Faltan)).ShouldBe(
            [("R-000001", "Faltan 2 documentos"), ("R-000002", "Falta 1 documento")]);
    }

    [Fact]
    public void Desde_HoraDeSolicitudEnUtc_LaMuestraEnHoraDeMexico()
    {
        var vm = SolicitudesPendientesViewModel.Desde([Pendiente(1, "R-000001", "B/L")], null, Mexico);

        vm.Grupos.ShouldHaveSingleItem().Solicitudes.ShouldHaveSingleItem().Solicitada
            .ShouldBe(new DateTime(2026, 11, 24, 12, 0, 0));   // 18:00 UTC = 12:00 en CDMX (UTC-6, sin horario de verano)
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Desde_NotaVacia_NoMuestraNota(string? nota)
    {
        var vm = SolicitudesPendientesViewModel.Desde([Pendiente(1, "R-000001", "B/L", nota)], null, Mexico);

        vm.Grupos[0].Solicitudes[0].Nota.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, "Ya tenemos todo lo que te pedimos")]
    [InlineData(1, "Aún falta 1 documento.")]
    [InlineData(2, "Aún faltan 2 documentos.")]
    public void Confirmacion_DiceCuantosDocumentosFaltan(int remaining, string expected)
    {
        var mensaje = SubirSolicitudViewModel.Confirmacion(
            new DocumentRequestAttendedDto("R-000007", "Lista de empaque", remaining, ReturnedToReview: remaining == 0));

        mensaje.ShouldStartWith("Recibimos \"Lista de empaque\" de la referencia R-000007.");
        mensaje.ShouldContain(expected);
    }
}
