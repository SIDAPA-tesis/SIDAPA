using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Exceptions.CaseFile;
using SIDAPA.domain.Rules.CaseFile;
using static SIDAPA.domain.Enums.Core.ReferenciaStatusEnum;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary>Pruebas de <see cref="DocumentRequestRule"/> (SCRUM-64 · HU-05): cuándo se atiende una solicitud y cuándo regresa a revisión.</summary>
public class DocumentRequestRuleTests
{
    [Fact]
    public void EnsureCanAttend_SolicitudAbiertaYReferenciaEnEspera_NoLanza()
    {
        Should.NotThrow(() => DocumentRequestRule.EnsureCanAttend(EN_ESPERA_DE_DOCUMENTOS, alreadyAttended: false));
    }

    [Fact]
    public void EnsureCanAttend_SolicitudYaAtendida_Lanza()
    {
        Should.Throw<DocumentRequestAlreadyAttendedException>(() =>
            DocumentRequestRule.EnsureCanAttend(EN_ESPERA_DE_DOCUMENTOS, alreadyAttended: true));
    }

    [Fact]
    public void EnsureCanAttend_YaAtendidaYReferenciaEnOtroEstatus_GanaYaAtendida()
    {
        // el cliente que subió el último documento y recarga la página ve "ya lo recibimos", no "ya no espera documentos"
        Should.Throw<DocumentRequestAlreadyAttendedException>(() =>
            DocumentRequestRule.EnsureCanAttend(EN_REVISION, alreadyAttended: true));
    }

    [Theory]
    [InlineData(RECIBIDA)]
    [InlineData(EN_REVISION)]
    [InlineData(EXPEDIENTE_COMPLETO)]
    [InlineData(CERRADO)]
    public void EnsureCanAttend_ReferenciaQueNoEsperaDocumentos_Lanza(ReferenciaStatusEnum status)
    {
        Should.Throw<ReferenciaNotAwaitingDocumentsException>(() =>
            DocumentRequestRule.EnsureCanAttend(status, alreadyAttended: false));
    }

    [Theory]
    [InlineData(0, true)]    // ya no queda ninguna: regresa a revisión
    [InlineData(1, false)]
    [InlineData(3, false)]
    public void ShouldReturnToReview_SoloCuandoNoQuedanAbiertas(int openRequests, bool expected)
    {
        DocumentRequestRule.ShouldReturnToReview(openRequests).ShouldBe(expected);
    }

    [Fact]
    public void ShouldReturnToReview_ConteoNegativo_Lanza()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DocumentRequestRule.ShouldReturnToReview(-1));
    }
}
