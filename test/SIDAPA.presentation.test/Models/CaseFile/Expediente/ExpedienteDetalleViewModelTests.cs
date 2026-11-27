using SIDAPA.application.DTOs.CaseFile.Expediente;
using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.CaseFile.Expediente;
using static SIDAPA.domain.Enums.Core.DocumentStatusEnum;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.presentation.test.Models.CaseFile.Expediente;

public class ExpedienteDetalleViewModelTests
{
    private static readonly TimeZoneInfo Mexico = TimeZoneInfo.CreateCustomTimeZone("MX", TimeSpan.FromHours(-6), "MX", "MX");

    private static ExpedienteReviewDto Review(ReferenciaStatusEnum status, params ChecklistItemDto[] checklist) => new(
        new ExpedienteDetailDto(7, "R-000007", "Importadora Norte", "AICM", "Importación", status, status.ToString(), null, null, null,
            new DateTime(2026, 11, 23, 18, 0, 0),
            [new ExpedienteDocumentDto(11, LISTA_EMPAQUE, "Lista", "lista.xlsx", 1536, RECIBIDO, "Cliente", new DateTime(2026, 11, 23, 18, 5, 0), null)],
            []),
        checklist,
        CanBeReviewed: status is ReferenciaStatusEnum.RECIBIDA or ReferenciaStatusEnum.EN_REVISION);

    [Fact]
    public void Completar_CuentaObligatoriosCorrectosYConvierteLaHora()
    {
        var model = new ExpedienteDetalleViewModel().Completar(Review(ReferenciaStatusEnum.EN_REVISION,
                new(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, "Factura", true, CORRECTO),
                new(LISTA_EMPAQUE, "Lista", true, RECIBIDO),
                new(POLIZA_SEGURO_TRANSPORTE, "Póliza", false, CORRECTO)),
            vistaPrevia: null, puedeMarcar: true, Mexico);

        model.Obligatorios.ShouldBe(2);
        model.ObligatoriosCorrectos.ShouldBe(1);
        model.Recibida.ShouldBe(new DateTime(2026, 11, 23, 12, 0, 0));
        model.Documentos.ShouldHaveSingleItem().Tamano.ShouldBe("1.5 KB");
        model.PuedeMarcar.ShouldBeTrue();
    }

    [Fact]
    public void Completar_ConPermisoPeroFueraDeRevision_NoPuedeMarcar()
    {
        var model = new ExpedienteDetalleViewModel().Completar(
            Review(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO), null, puedeMarcar: true, Mexico);

        model.PuedeMarcar.ShouldBeFalse();
        model.Documentos.ShouldAllBe(d => !d.PuedeMarcar);
    }

    [Fact]
    public void Completar_VistaPreviaDeOtraReferencia_SeIgnora()
    {
        var ajena = new DocumentPreviewDto(99, ReferenciaId: 8, "ajeno.pdf", DocumentPreviewKindEnum.Pdf, null);

        var model = new ExpedienteDetalleViewModel().Completar(Review(ReferenciaStatusEnum.EN_REVISION), ajena, true, Mexico);

        model.VistaPrevia.ShouldBeNull();
    }

    [Fact]
    public void Completar_VistaPreviaPropia_MarcaElDocumentoAbierto()
    {
        var propia = new DocumentPreviewDto(11, ReferenciaId: 7, "lista.xlsx", DocumentPreviewKindEnum.Spreadsheet, null);

        var model = new ExpedienteDetalleViewModel().Completar(Review(ReferenciaStatusEnum.EN_REVISION), propia, true, Mexico);

        var documento = model.Documentos.ShouldHaveSingleItem();
        documento.Seleccionado.ShouldBeTrue();
        documento.VistaPreviaAbierta.ShouldBe(11);
    }

    [Theory]
    [InlineData(true, 0, "Todos los documentos obligatorios están correctos: la referencia pasó a Expediente completo.")]
    [InlineData(false, 0, "Documento marcado como Con error.")]
    [InlineData(false, 1, "Documento marcado como Con error. Falta 1 documento obligatorio por marcar como correcto.")]
    [InlineData(false, 2, "Documento marcado como Con error. Faltan 2 documentos obligatorios por marcar como correctos.")]
    public void Confirmacion_DiceQueCambioYCuantoFalta(bool changed, int missing, string expected)
    {
        var resultado = new ExpedienteStatusDto(7, ReferenciaStatusEnum.EN_REVISION, [],
            Enumerable.Repeat(LISTA_EMPAQUE, missing).ToList(), IsComplete: false, changed);

        ExpedienteDetalleViewModel.Confirmacion(resultado, ERROR).ShouldBe(expected);
    }

    [Fact]
    public void Requisito_OpcionalSinArchivo_NoSePintaComoFaltante()
    {
        var opcional = RequisitoExpedienteViewModel.Desde(new(POLIZA_SEGURO_TRANSPORTE, "Póliza", false, FALTANTE));
        var obligatorio = RequisitoExpedienteViewModel.Desde(new(LISTA_EMPAQUE, "Lista", true, FALTANTE));

        (opcional.Etiqueta, opcional.Color).ShouldBe(("No recibido", "secondary"));
        (obligatorio.Etiqueta, obligatorio.Color).ShouldBe(("Faltante", "warning"));
    }

    [Theory]
    [InlineData(1, 1, new[] { 1 })]
    [InlineData(1, 3, new[] { 1, 2, 3 })]
    [InlineData(1, 9, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(5, 9, new[] { 3, 4, 5, 6, 7 })]
    [InlineData(9, 9, new[] { 5, 6, 7, 8, 9 })]
    public void Bandeja_PaginasVisibles_CincoAlrededorDeLaActual(int pagina, int totalPaginas, int[] esperadas)
    {
        var bandeja = new BandejaExpedientesViewModel([], null, false, pagina, totalPaginas, totalPaginas * 20);

        bandeja.PaginasVisibles.ShouldBe(esperadas);
    }

    [Fact]
    public void Bandeja_Ruta_SoloLlevaLosFiltrosQueSeUsan()
    {
        new BandejaExpedientesViewModel([], null, false, 1, 2, 30).Ruta(2).ShouldBe(new Dictionary<string, string> { ["pagina"] = "2" });
        new BandejaExpedientesViewModel([], "norte", true, 1, 2, 30).Ruta(2).Keys.ShouldBe(["pagina", "q", "porRevisar"]);
    }
}
