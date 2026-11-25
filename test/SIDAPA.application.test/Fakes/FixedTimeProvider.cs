namespace SIDAPA.application.test.Fakes;

/// <summary>
/// Reloj fijo para servicios que reciben <see cref="TimeProvider"/>. Así las fechas guardadas son
/// deterministas y se pueden afirmar exactas. <see cref="Advance"/> mueve el reloj a mano.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 11, 23, 15, 30, 0, TimeSpan.Zero);

    private DateTimeOffset _now = now;

    public FixedTimeProvider() : this(Default) { }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
