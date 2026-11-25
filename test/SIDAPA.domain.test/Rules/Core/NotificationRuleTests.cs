using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.Core;

/// <summary>Pruebas de <see cref="NotificationRule"/>: el texto de un aviso (SCRUM-59 · HU-04-T02).</summary>
public class NotificationRuleTests
{
    [Theory]
    [InlineData("Hola", "Hola")]
    [InlineData("  Hola  ", "Hola")]                 // se recorta antes de guardar
    [InlineData("\tLínea 1\nLínea 2\n", "Línea 1\nLínea 2")] // los saltos internos se conservan
    public void EnsureValidMessage_Valido_RegresaElTextoRecortado(string message, string expected) =>
        NotificationRule.EnsureValidMessage(message).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void EnsureValidMessage_VacioOSoloEspacios_Lanza(string? message) =>
        Should.Throw<EmptyNotificationMessageException>(() => NotificationRule.EnsureValidMessage(message));

    [Fact]
    public void EnsureValidMessage_EnElLimiteExacto_Pasa()
    {
        var message = new string('x', NotificationRule.MaxMessageLength);

        NotificationRule.EnsureValidMessage(message).ShouldBe(message);
    }

    [Fact]
    public void EnsureValidMessage_UnoMasDelLimite_Lanza() =>
        Should.Throw<NotificationMessageTooLongException>(() =>
            NotificationRule.EnsureValidMessage(new string('x', NotificationRule.MaxMessageLength + 1)));

    // El límite se mide después de recortar: 500 caracteres útiles rodeados de espacios sí pasan.
    [Fact]
    public void EnsureValidMessage_LimiteConEspacios_SeMideRecortado()
    {
        var text = new string('x', NotificationRule.MaxMessageLength);

        NotificationRule.EnsureValidMessage($"  {text}  ").ShouldBe(text);
    }

    [Fact]
    public void MaxMessageLength_Es500() => NotificationRule.MaxMessageLength.ShouldBe(500);
}
