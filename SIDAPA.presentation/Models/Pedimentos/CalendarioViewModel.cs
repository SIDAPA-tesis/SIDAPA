using System.Globalization;
using SIDAPA.application.DTOs.Core.Pedimento;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.presentation.Models.Pedimentos
{
    public record DiaCalendario(DateOnly Fecha, bool EnMes, bool EsHoy, IReadOnlyList<PedimentoDto> Vencimientos)
    {
        /// <summary>La urgencia más grave entre los pedimentos que vencen ese día.</summary>
        public UrgenciaEnum? Urgencia => Vencimientos.Count > 0 ? Vencimientos.Max(v => v.Urgencia) : null;
    }

    public class CalendarioViewModel
    {
        // La semana inicia en lunes. Si la prefieres en domingo, cambia DiasSemana y el cálculo de "desfase".
        public static readonly string[] DiasSemana = ["lun", "mar", "mié", "jue", "vie", "sáb", "dom"];

        private static readonly string[] Meses =
        [
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        ];

        public required string Titulo { get; init; }
        public required string MesAnterior { get; init; }
        public required string MesActual { get; init; }
        public required string MesSiguiente { get; init; }
        public string? Busqueda { get; init; }
        public required IReadOnlyList<DiaCalendario> Dias { get; init; }
        public required IReadOnlyList<PedimentoDto> Vencimientos { get; init; }

        public static CalendarioViewModel Crear(DateOnly mes, DateOnly hoy, IReadOnlyList<PedimentoDto> pedimentos, string? busqueda)
        {
            var primero = new DateOnly(mes.Year, mes.Month, 1);
            var desfase = ((int)primero.DayOfWeek + 6) % 7;
            var diasDelMes = DateTime.DaysInMonth(primero.Year, primero.Month);
            var celdas = (int)Math.Ceiling((desfase + diasDelMes) / 7.0) * 7;
            var inicio = primero.AddDays(-desfase);

            var porFecha = pedimentos.ToLookup(p => p.FechaLimite);

            var dias = Enumerable.Range(0, celdas)
                .Select(i => inicio.AddDays(i))
                .Select(f =>
                {
                    var enMes = f.Month == primero.Month;
                    IReadOnlyList<PedimentoDto> vencen = enMes ? porFecha[f].ToList() : [];
                    return new DiaCalendario(f, enMes, f == hoy, vencen);
                })
                .ToList();

            return new CalendarioViewModel
            {
                Titulo = $"{Meses[primero.Month - 1]} {primero.Year}",
                MesAnterior = Formato(primero.AddMonths(-1)),
                MesActual = Formato(primero),
                MesSiguiente = Formato(primero.AddMonths(1)),
                Busqueda = busqueda,
                Dias = dias,
                Vencimientos = pedimentos
                    .Where(p => p.FechaLimite.Year == primero.Year && p.FechaLimite.Month == primero.Month)
                    .OrderBy(p => p.FechaLimite)
                    .ToList()
            };
        }

        /// <summary>Valores de ruta para los enlaces de mes anterior/siguiente, conservando la búsqueda.</summary>
        public Dictionary<string, string> Ruta(string mes)
        {
            var ruta = new Dictionary<string, string> { ["mes"] = mes };
            if (!string.IsNullOrWhiteSpace(Busqueda)) ruta["q"] = Busqueda;
            return ruta;
        }

        private static string Formato(DateOnly fecha) => fecha.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

}
