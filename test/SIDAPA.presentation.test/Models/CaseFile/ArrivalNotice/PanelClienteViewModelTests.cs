using SIDAPA.application.DTOs.Core.Referencias;
using SIDAPA.domain.Enums.Core;
using SIDAPA.presentation.Models.CaseFile.ArrivalNotice;

namespace SIDAPA.presentation.test.Models.CaseFile.ArrivalNotice;

/// <summary>
/// Panel "Mis avisos de arribo" (HU-02 / SCRUM-46): la base guarda la creación en UTC y el panel la muestra
/// en la hora local, para que el Cliente no vea su aviso seis horas "en el futuro".
/// </summary>
public class PanelClienteViewModelTests
{
    private static readonly TimeZoneInfo Mexico = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    private static ClientReferenciaDto Referencia(string numero, DateTime createdAtUtc) =>
        new(1, numero, "ACAPULCO", null, ReferenciaStatusEnum.RECIBIDA, "Recibida", createdAtUtc, 2);

    [Fact]
    public void Filas_CreacionEnUtc_LaMuestraEnHoraDeMexico()
    {
        // Así llega de la columna timestamp without time zone: Kind Unspecified, valor en UTC.
        var utc = new DateTime(2026, 11, 24, 23, 25, 0, DateTimeKind.Unspecified);
        var panel = new PanelClienteViewModel([Referencia("R-000001", utc)], null, Mexico);

        var fila = panel.Filas.ShouldHaveSingleItem();

        fila.Creada.ShouldBe(new DateTime(2026, 11, 24, 17, 25, 0));
    }

    [Fact]
    public void Filas_CreacionDespuesDeMedianocheUtc_CambiaAlDiaAnterior()
    {
        var utc = new DateTime(2026, 11, 25, 3, 10, 0, DateTimeKind.Unspecified);
        var panel = new PanelClienteViewModel([Referencia("R-000002", utc)], null, Mexico);

        var fila = panel.Filas.ShouldHaveSingleItem();

        fila.Creada.ShouldBe(new DateTime(2026, 11, 24, 21, 10, 0));
    }

    [Fact]
    public void Filas_ReferenciaRecienCreada_SoloEsaQuedaDestacada()
    {
        var utc = new DateTime(2026, 11, 24, 12, 0, 0);
        var panel = new PanelClienteViewModel(
            [Referencia("R-000001", utc), Referencia("R-000002", utc)], "R-000002", Mexico);

        var destacadas = panel.Filas.Where(f => f.Destacada).Select(f => f.Referencia.ReferenciaNumber);

        destacadas.ShouldBe(["R-000002"]);
    }
}
