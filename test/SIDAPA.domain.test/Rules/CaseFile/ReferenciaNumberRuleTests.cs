using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary>Formato del número de referencia (SCRUM-46).</summary>
public class ReferenciaNumberRuleTests
{
    [Theory]
    [InlineData(1, "R-000001")]
    [InlineData(123, "R-000123")]
    [InlineData(999999, "R-999999")]     // límite: el último que cabe en 6 dígitos
    [InlineData(1234567, "R-1234567")]   // pasado el límite crece, no se trunca
    public void Format_RellenaASeisDigitos(long consecutive, string expected) =>
        ReferenciaNumberRule.Format(consecutive).ShouldBe(expected);
}
