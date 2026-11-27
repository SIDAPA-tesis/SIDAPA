using System.Globalization;
using System.Text;
using ExcelDataReader;
using SIDAPA.application.DTOs.CaseFile.Documents;
using SIDAPA.application.Interface.CaseFile.Documents;

namespace SIDAPA.infraestructure.External.Spreadsheets;

/// <summary>
/// Vista previa de XLS/XLSX con ExcelDataReader (detecta el formato por el contenido). Solo lee los primeros
/// <see cref="MaxSheets"/> × <see cref="MaxRows"/> × <see cref="MaxColumns"/>: una hoja de miles de filas no cabe en la página.
/// </summary>
public class ExcelSpreadsheetPreviewReader : ISpreadsheetPreviewReader
{
    public const int MaxSheets = 10;
    public const int MaxRows = 200;
    public const int MaxColumns = 30;

    // Los .xls (BIFF) usan code pages de Windows que .NET no trae registradas por defecto.
    static ExcelSpreadsheetPreviewReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public SpreadsheetPreviewDto Read(Stream content)
    {
        using var reader = ExcelReaderFactory.CreateReader(content);
        var sheets = new List<SheetPreviewDto>();
        var truncated = false;

        do
        {
            if (sheets.Count == MaxSheets)
            {
                truncated = true;
                break;
            }

            var rows = new List<IReadOnlyList<string>>();
            while (reader.Read())
            {
                if (rows.Count == MaxRows)
                {
                    truncated = true;
                    break;
                }

                if (reader.FieldCount > MaxColumns)
                    truncated = true;

                var row = Enumerable.Range(0, Math.Min(reader.FieldCount, MaxColumns))
                    .Select(i => Format(reader.GetValue(i)))
                    .ToList();
                rows.Add(row);
            }

            sheets.Add(new SheetPreviewDto(reader.Name, TrimEmptyTail(rows)));
        } while (reader.NextResult());

        return new SpreadsheetPreviewDto(sheets, truncated);
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        DateTime d => d.TimeOfDay == TimeSpan.Zero
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    // Excel suele reportar filas vacías al final (formato aplicado a celdas sin datos).
    private static List<IReadOnlyList<string>> TrimEmptyTail(List<IReadOnlyList<string>> rows)
    {
        var count = rows.Count;
        while (count > 0 && rows[count - 1].All(string.IsNullOrEmpty))
            count--;
        return rows.GetRange(0, count);
    }
}
