using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;
using static SIDAPA.domain.Enums.Core.DocumentStatusEnum;
using static SIDAPA.domain.Enums.Core.DocumentTypeEnum;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary><see cref="ExpedienteRule.ChecklistStatus"/>: renglón de la lista de verificación (HU-03).</summary>
public class ExpedienteChecklistTests
{
    [Fact]
    public void SinArchivoDeEseTipo_EsFaltante() =>
        ExpedienteRule.ChecklistStatus(LISTA_EMPAQUE, [(DOCUMENTO_TRANSPORTE, CORRECTO)]).ShouldBe(FALTANTE);

    [Theory]
    [InlineData(ERROR, CORRECTO, CORRECTO)]     // basta uno bueno
    [InlineData(ERROR, RECIBIDO, RECIBIDO)]     // el reemplazo sin revisar pesa más que el rechazado
    [InlineData(FALTANTE, ERROR, ERROR)]
    [InlineData(FALTANTE, FALTANTE, FALTANTE)]
    public void VariosArchivos_GanaElDeMayorPrioridad(DocumentStatusEnum primero, DocumentStatusEnum segundo, DocumentStatusEnum esperado) =>
        ExpedienteRule.ChecklistStatus(LISTA_EMPAQUE, [(LISTA_EMPAQUE, primero), (LISTA_EMPAQUE, segundo)]).ShouldBe(esperado);
}
