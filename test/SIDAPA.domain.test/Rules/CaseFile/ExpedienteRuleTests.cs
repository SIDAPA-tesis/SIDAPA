using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;
using static SIDAPA.domain.Enums.Core.DocumentStatusEnum;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary>Pruebas de la regla de expediente completo (SCRUM-56).</summary>
public class ExpedienteRuleTests
{
    private static readonly DocumentTypeEnum[] Mandatory = [FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DOCUMENTO_TRANSPORTE];

    [Fact]
    public void TodosCorrectos_EstaCompleto()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), (DOCUMENTO_TRANSPORTE, CORRECTO)]);

        missing.ShouldBeEmpty();
        ExpedienteRule.IsComplete(Mandatory, missing).ShouldBeTrue();
    }

    [Fact]
    public void UnoConError_FaltaEseDocumento()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), (DOCUMENTO_TRANSPORTE, ERROR)]);

        missing.ShouldHaveSingleItem().ShouldBe(DOCUMENTO_TRANSPORTE);
        ExpedienteRule.IsComplete(Mandatory, missing).ShouldBeFalse();
    }

    [Fact]
    public void RecibidoSinRevisar_NoCuentaComoCorrecto()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO), (DOCUMENTO_TRANSPORTE, CORRECTO)]);

        missing.ShouldHaveSingleItem().ShouldBe(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR);
    }

    [Fact]
    public void SiSeVolvioASubirYAhoraEsCorrecto_EstaCompleto()
    {
        // Primero subió una factura con error y después la buena.
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
        [
            (FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, ERROR),
            (FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO),
            (DOCUMENTO_TRANSPORTE, CORRECTO)
        ]);

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void DocumentosOpcionales_NoAfectan()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
        [
            (FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO),
            (DOCUMENTO_TRANSPORTE, CORRECTO),
            (POLIZA_SEGURO_TRANSPORTE, FALTANTE)
        ]);

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void SinDocumentosObligatoriosConfigurados_NoSeDaPorCompleto()
    {
        ExpedienteRule.IsComplete([], []).ShouldBeFalse();
    }
}
