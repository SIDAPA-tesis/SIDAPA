using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;
using Xunit;
using static SIDAPA.domain.Enums.Core.DocumentStatusEnum;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.Infraestructure.test;

/// <summary>Pruebas de la regla de expediente completo (SCRUM-56).</summary>
public class ExpedienteRuleTests
{
    private static readonly DocumentTypeEnum[] Mandatory = [FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, DOCUMENTO_TRANSPORTE];

    [Fact]
    public void TodosCorrectos_EstaCompleto()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), (DOCUMENTO_TRANSPORTE, CORRECTO)]);

        Assert.Empty(missing);
        Assert.True(ExpedienteRule.IsComplete(Mandatory, missing));
    }

    [Fact]
    public void UnoConError_FaltaEseDocumento()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, CORRECTO), (DOCUMENTO_TRANSPORTE, ERROR)]);

        Assert.Equal(DOCUMENTO_TRANSPORTE, Assert.Single(missing));
        Assert.False(ExpedienteRule.IsComplete(Mandatory, missing));
    }

    [Fact]
    public void RecibidoSinRevisar_NoCuentaComoCorrecto()
    {
        var missing = ExpedienteRule.MissingDocuments(Mandatory,
            [(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, RECIBIDO), (DOCUMENTO_TRANSPORTE, CORRECTO)]);

        Assert.Equal(FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, Assert.Single(missing));
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

        Assert.Empty(missing);
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

        Assert.Empty(missing);
    }

    [Fact]
    public void SinDocumentosObligatoriosConfigurados_NoSeDaPorCompleto()
    {
        Assert.False(ExpedienteRule.IsComplete([], []));
    }
}