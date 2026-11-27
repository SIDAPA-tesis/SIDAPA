using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.domain.Rules.CaseFile;

namespace SIDAPA.domain.test.Rules.CaseFile;

public class DocumentReviewRuleTests
{
    [Theory]
    [InlineData(ReferenciaStatusEnum.RECIBIDA)]
    [InlineData(ReferenciaStatusEnum.EN_REVISION)]
    public void RecibidaOEnRevision_SePuedeRevisar(ReferenciaStatusEnum status)
    {
        DocumentReviewRule.CanReview(status).ShouldBeTrue();
        Should.NotThrow(() => DocumentReviewRule.EnsureCanReview(status));
    }

    [Theory]
    [InlineData(ReferenciaStatusEnum.EN_ESPERA_DE_DOCUMENTOS)]
    [InlineData(ReferenciaStatusEnum.EXPEDIENTE_COMPLETO)]
    [InlineData(ReferenciaStatusEnum.CERRADO)]
    public void OtroEstatus_NoSePuedeRevisar(ReferenciaStatusEnum status)
    {
        DocumentReviewRule.CanReview(status).ShouldBeFalse();
        Should.Throw<ExpedienteNotInReviewException>(() => DocumentReviewRule.EnsureCanReview(status));
    }

    [Theory]
    [InlineData(DocumentStatusEnum.CORRECTO)]
    [InlineData(DocumentStatusEnum.FALTANTE)]
    [InlineData(DocumentStatusEnum.ERROR)]
    public void MarcaDeRevision_EsValida(DocumentStatusEnum status) =>
        Should.NotThrow(() => DocumentReviewRule.EnsureValidMark(status));

    [Theory]
    [InlineData(DocumentStatusEnum.RECIBIDO)]
    [InlineData((DocumentStatusEnum)99)]
    public void RecibidoOValorDesconocido_SeRechaza(DocumentStatusEnum status) =>
        Should.Throw<InvalidDocumentReviewStatusException>(() => DocumentReviewRule.EnsureValidMark(status));

    [Theory]
    [InlineData(ReferenciaStatusEnum.RECIBIDA, true)]
    [InlineData(ReferenciaStatusEnum.EN_REVISION, false)]
    public void SoloRecibida_ComienzaLaRevision(ReferenciaStatusEnum status, bool starts) =>
        DocumentReviewRule.StartsReview(status).ShouldBe(starts);
}
