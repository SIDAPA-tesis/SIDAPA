using System.IO.Compression;
using System.Security;
using System.Text;
using SIDAPA.infraestructure.External.Spreadsheets;

namespace SIDAPA.infraestructure.test.External.Spreadsheets;

/// <summary>
/// <see cref="ExcelSpreadsheetPreviewReader"/> con un .xlsx real armado en memoria (sin archivos de prueba en el repo).
/// </summary>
public class ExcelSpreadsheetPreviewReaderTests
{
    private readonly ExcelSpreadsheetPreviewReader _reader = new();

    [Fact]
    public void Read_HojaChica_RegresaSuNombreYSusCeldasComoTexto()
    {
        using var xlsx = Xlsx(("Lista", [["Caja", "Piezas"], ["A-1", 1.5], ["A-2", 10]]));

        var preview = _reader.Read(xlsx);

        preview.Truncated.ShouldBeFalse();
        var sheet = preview.Sheets.ShouldHaveSingleItem();
        sheet.Name.ShouldBe("Lista");
        sheet.Rows.Select(r => string.Join("|", r)).ShouldBe(["Caja|Piezas", "A-1|1.5", "A-2|10"]);   // punto decimal siempre
    }

    [Fact]
    public void Read_VariasHojas_LasRegresaEnOrden()
    {
        using var xlsx = Xlsx(("Factura", [["F-1"]]), ("Empaque", [["E-1"]]));

        _reader.Read(xlsx).Sheets.Select(s => s.Name).ShouldBe(["Factura", "Empaque"]);
    }

    [Fact]
    public void Read_MasFilasQueElTope_RecortaYLoAvisa()
    {
        var rows = Enumerable.Range(1, ExcelSpreadsheetPreviewReader.MaxRows + 5).Select(i => new object[] { i }).ToArray();
        using var xlsx = Xlsx(("Grande", rows));

        var preview = _reader.Read(xlsx);

        preview.Truncated.ShouldBeTrue();
        preview.Sheets.ShouldHaveSingleItem().Rows.Count.ShouldBe(ExcelSpreadsheetPreviewReader.MaxRows);
    }

    [Fact]
    public void Read_MasColumnasQueElTope_RecortaYLoAvisa()
    {
        var row = Enumerable.Range(1, ExcelSpreadsheetPreviewReader.MaxColumns + 3).Cast<object>().ToArray();
        using var xlsx = Xlsx(("Ancha", [row]));

        var preview = _reader.Read(xlsx);

        preview.Truncated.ShouldBeTrue();
        preview.Sheets.ShouldHaveSingleItem().Rows.ShouldHaveSingleItem().Count.ShouldBe(ExcelSpreadsheetPreviewReader.MaxColumns);
    }

    [Fact]
    public void Read_MasHojasQueElTope_RecortaYLoAvisa()
    {
        var sheets = Enumerable.Range(1, ExcelSpreadsheetPreviewReader.MaxSheets + 1)
            .Select(i => ($"H{i}", new[] { new object[] { i } }))
            .ToArray();
        using var xlsx = Xlsx(sheets);

        var preview = _reader.Read(xlsx);

        preview.Truncated.ShouldBeTrue();
        preview.Sheets.Count.ShouldBe(ExcelSpreadsheetPreviewReader.MaxSheets);
    }

    // ------------------------------------------------------------------ xlsx mínimo (Open XML a mano)

    private static MemoryStream Xlsx(params (string Name, object[][] Rows)[] sheets)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Entry(zip, "[Content_Types].xml",
                """<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>"""
                + string.Concat(sheets.Select((_, i) => $"""<Override PartName="/xl/worksheets/sheet{i + 1}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>"""))
                + "</Types>");
            Entry(zip, "_rels/.rels",
                """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Entry(zip, "xl/workbook.xml",
                """<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>"""
                + string.Concat(sheets.Select((s, i) => $"""<sheet name="{SecurityElement.Escape(s.Name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>"""))
                + "</sheets></workbook>");
            Entry(zip, "xl/_rels/workbook.xml.rels",
                """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">"""
                + string.Concat(sheets.Select((_, i) => $"""<Relationship Id="rId{i + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i + 1}.xml"/>"""))
                + "</Relationships>");

            for (var i = 0; i < sheets.Length; i++)
                Entry(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i].Rows));
        }

        stream.Position = 0;
        return stream;
    }

    private static string Sheet(object[][] rows)
    {
        var xml = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        for (var r = 0; r < rows.Length; r++)
        {
            xml.Append($"""<row r="{r + 1}">""");
            for (var c = 0; c < rows[r].Length; c++)
            {
                var cell = $"{Column(c)}{r + 1}";
                xml.Append(rows[r][c] is string text
                    ? $"""<c r="{cell}" t="inlineStr"><is><t>{SecurityElement.Escape(text)}</t></is></c>"""
                    : $"""<c r="{cell}"><v>{Convert.ToString(rows[r][c], System.Globalization.CultureInfo.InvariantCulture)}</v></c>""");
            }
            xml.Append("</row>");
        }
        return xml.Append("</sheetData></worksheet>").ToString();
    }

    // 0 → A, 25 → Z, 26 → AA
    private static string Column(int index) =>
        index < 26 ? ((char)('A' + index)).ToString() : Column(index / 26 - 1) + (char)('A' + index % 26);

    private static void Entry(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
