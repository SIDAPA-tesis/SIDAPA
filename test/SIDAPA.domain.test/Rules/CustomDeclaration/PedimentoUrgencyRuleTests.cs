using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.CustomDeclaration;

/// <summary>Avance y urgencia del panel de pedimentos.</summary>
public class PedimentoUrgencyRuleTests
{
    [Theory]
    [InlineData(ReferenciaStatusEnum.RECIBIDA, 0)]
    [InlineData(ReferenciaStatusEnum.CERRADO, 100)]
    [InlineData(ReferenciaStatusEnum.RECHAZADO_POR_VALIDADOR, 57)]   // retrabajo = avance de EN_GLOSA
    [InlineData(ReferenciaStatusEnum.CON_ANOTACIONES, 57)]
    public void ProgressFor(ReferenciaStatusEnum status, int expected) =>
        PedimentoUrgencyRule.ProgressFor(status).ShouldBe(expected);

    [Fact]
    public void ProgressFor_RechazadoNoAvanzaMasQueGlosado() =>
        PedimentoUrgencyRule.ProgressFor(ReferenciaStatusEnum.RECHAZADO_POR_VALIDADOR)
            .ShouldBeLessThan(PedimentoUrgencyRule.ProgressFor(ReferenciaStatusEnum.GLOSADO));
}
