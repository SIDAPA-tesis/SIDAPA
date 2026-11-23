namespace SIDAPA.domain.Rules.Core;

/// <summary>Número de referencia que se le da al cliente: R- y un consecutivo de 6 dígitos (R-000123).</summary>
public static class ReferenciaNumberRule
{
    public const string Prefix = "R-";

    public static string Format(long consecutive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(consecutive);
        return $"{Prefix}{consecutive:D6}";
    }
}
