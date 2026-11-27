using System.Text;
using SIDAPA.domain.Enums.CaseFile;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary>
/// Pruebas de <see cref="DocumentFileRule"/> (SCRUM-50): tipos permitidos, tamaño máximo y firma del contenido
/// de los documentos de una referencia. La regla de negocio se prueba aquí; el validador de application solo
/// lee el encabezado del Stream.
/// </summary>
public class DocumentFileRuleTests
{
    private const long OneKb = 1024;

    // Firmas reales (primeros bytes) de cada tipo permitido y de un ejecutable de Windows.
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7");
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];          // .xlsx (y .docx, .zip…)
    private static readonly byte[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];          // .xls
    private static readonly byte[] Xml = Encoding.ASCII.GetBytes("<?xml ve");
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];          // "MZ"

    private static void Validate(string fileName, byte[] header, long sizeInBytes = OneKb) =>
        DocumentFileRule.EnsureValid(fileName, sizeInBytes, header);

    // ---------------------------------------------------------------- Tipos permitidos

    public static TheoryData<string, byte[]> ArchivosValidos => new()
    {
        { "factura.pdf", Pdf },
        { "lista.xlsx", Zip },
        { "lista.xls", Ole },
        { "cfdi.xml", Xml },
        { "FACTURA.PDF", Pdf },                // la extensión se compara en minúsculas
        { "Lista Empaque.XlSx", Zip },
        { "mi.factura.final.pdf", Pdf },       // solo cuenta la última extensión
    };

    [Theory]
    [MemberData(nameof(ArchivosValidos))]
    public void EnsureValid_TipoPermitidoConSuFirma_Pasa(string fileName, byte[] header) =>
        Should.NotThrow(() => Validate(fileName, header));

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("factura")]                  // sin extensión
    [InlineData("factura.")]                 // punto final, extensión vacía
    [InlineData("factura.pdf.exe")]          // doble extensión: manda la última
    [InlineData("contrato.docx")]
    [InlineData("datos.csv")]
    [InlineData("factura.txt")]
    [InlineData("factura.pdfx")]
    public void EnsureValid_ExtensionNoPermitida_LanzaTipoNoPermitido(string fileName) =>
        Should.Throw<FileTypeNotAllowedException>(() => Validate(fileName, Pdf));

    [Fact]
    public void EnsureValid_ExtensionNoPermitidaConFirmaValida_LanzaTipoNoPermitido()
    {
        // un PDF renombrado a .exe no se acepta aunque su contenido sea PDF: el tipo lo decide la extensión
        Should.Throw<FileTypeNotAllowedException>(() => Validate("factura.exe", Pdf));
    }

    [Fact]
    public void AllowedExtensions_SonExactamentePdfXmlXlsYXlsx()
    {
        // si alguien agrega un tipo, que sea a propósito (y actualice la firma en ContentMatches)
        DocumentFileRule.AllowedExtensions.ShouldBe([".pdf", ".xml", ".xls", ".xlsx"], ignoreOrder: true);
    }

    // ---------------------------------------------------------------- Contenido (firma)

    public static TheoryData<string, byte[]> ContenidoQueNoCorresponde => new()
    {
        { "bl.pdf", Exe },                     // .exe renombrado a .pdf
        { "lista.xlsx", Exe },
        { "lista.xls", Exe },
        { "cfdi.xml", Exe },
        { "lista.xlsx", Ole },                 // .xls viejo renombrado a .xlsx
        { "lista.xls", Zip },                  // .xlsx renombrado a .xls
        { "factura.pdf", Zip },
        { "cfdi.xml", Pdf },
    };

    [Theory]
    [MemberData(nameof(ContenidoQueNoCorresponde))]
    public void EnsureValid_ContenidoDeOtroTipo_LanzaContenidoNoCorresponde(string fileName, byte[] header) =>
        Should.Throw<FileContentMismatchException>(() => Validate(fileName, header));

    [Theory]
    [InlineData("factura.pdf")]
    [InlineData("lista.xlsx")]
    [InlineData("lista.xls")]
    [InlineData("cfdi.xml")]
    public void EnsureValid_SinEncabezadoPeroConTamano_LanzaContenidoNoCorresponde(string fileName) =>
        // el tamaño declarado dice que hay bytes, pero no llegó ninguno
        Should.Throw<FileContentMismatchException>(() => Validate(fileName, []));

    [Theory]
    [InlineData("factura.pdf", 4)]           // "%PDF" sin el guion
    [InlineData("lista.xlsx", 3)]
    [InlineData("lista.xls", 7)]             // la firma OLE ocupa los 8 bytes del encabezado
    public void EnsureValid_FirmaIncompleta_LanzaContenidoNoCorresponde(string fileName, int bytes)
    {
        var full = Path.GetExtension(fileName) switch { ".pdf" => Pdf, ".xlsx" => Zip, _ => Ole };

        Should.Throw<FileContentMismatchException>(() => Validate(fileName, full[..bytes]));
    }

    [Fact]
    public void EnsureValid_PdfMasCortoQueElEncabezado_Pasa()
    {
        // un archivo de 5 bytes con firma completa: el encabezado puede ser menor que HeaderLength
        var header = Encoding.ASCII.GetBytes("%PDF-");

        Should.NotThrow(() => Validate("mini.pdf", header, sizeInBytes: header.Length));
    }

    [Theory]
    [InlineData(new byte[] { 0x3C })]                                     // "<"
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x3C, 0x3F })]             // BOM UTF-8 + "<?"
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x3C })]             // espacios, tab y saltos antes de "<"
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x0A, 0x20, 0x3C })]       // BOM + salto + espacio + "<"
    public void EnsureValid_XmlConBomOEspaciosIniciales_Pasa(byte[] header) =>
        Should.NotThrow(() => Validate("cfdi.xml", header));

    [Theory]
    [InlineData(new byte[] { 0x20, 0x20, 0x0A, 0x09 })]                   // solo espacios
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF })]                         // solo BOM
    [InlineData(new byte[] { 0x7B, 0x22, 0x61, 0x22 })]                   // JSON: {"a"
    [InlineData(new byte[] { 0x66, 0x6F, 0x6F, 0x3C })]                   // texto antes de "<"
    public void EnsureValid_XmlQueNoEmpiezaConEtiqueta_LanzaContenidoNoCorresponde(byte[] header) =>
        Should.Throw<FileContentMismatchException>(() => Validate("cfdi.xml", header));

    // ---------------------------------------------------------------- Tamaño

    [Fact]
    public void EnsureValid_Vacio_LanzaArchivoVacio() =>
        Should.Throw<EmptyFileException>(() => Validate("factura.pdf", [], sizeInBytes: 0));

    [Fact]
    public void EnsureValid_VacioConEncabezado_LanzaArchivoVacio()
    {
        // manda el tamaño declarado, no los bytes leídos
        Should.Throw<EmptyFileException>(() => Validate("factura.pdf", Pdf, sizeInBytes: 0));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(DocumentFileRule.maxSizeInBytes - 1)]
    [InlineData(DocumentFileRule.maxSizeInBytes)]                          // límite exacto: 20 MB sí pasa
    public void EnsureValid_HastaElLimite_Pasa(long sizeInBytes) =>
        Should.NotThrow(() => Validate("factura.pdf", Pdf, sizeInBytes));

    [Theory]
    [InlineData(DocumentFileRule.maxSizeInBytes + 1)]                      // un byte de más
    [InlineData(DocumentFileRule.maxSizeInBytes * 5)]
    [InlineData(long.MaxValue)]
    public void EnsureValid_MasDelLimite_LanzaArchivoMuyGrande(long sizeInBytes) =>
        Should.Throw<FileTooLargeException>(() => Validate("factura.pdf", Pdf, sizeInBytes));

    [Fact]
    public void MaxSizeInBytes_Son20Mb()
    {
        // 20 MB binarios (20 × 1024 × 1024), como dice docs/aviso-de-arribo.md
        DocumentFileRule.maxSizeInBytes.ShouldBe(20L * 1024 * 1024);
    }

    [Fact]
    public void EnsureValid_MuyGrande_ElMensajeDiceElMaximo()
    {
        var ex = Should.Throw<FileTooLargeException>(() =>
            Validate("factura.pdf", Pdf, sizeInBytes: 25L * 1024 * 1024));

        ex.Message.ShouldContain("pesa 25 MB");
        ex.Message.ShouldContain("el maximo es 20 MB");
    }

    // ---------------------------------------------------------------- Orden de las reglas y mensajes

    [Fact]
    public void EnsureValid_ExeVacio_ReportaPrimeroElTipo() =>
        // el tipo se revisa antes que el tamaño: al usuario se le dice que el .exe no se acepta
        Should.Throw<FileTypeNotAllowedException>(() => Validate("setup.exe", [], sizeInBytes: 0));

    [Fact]
    public void EnsureValid_PdfMuyGrandeYConContenidoAjeno_ReportaPrimeroElTamano() =>
        // el tamaño se revisa antes que la firma
        Should.Throw<FileTooLargeException>(() =>
            Validate("bl.pdf", Exe, sizeInBytes: DocumentFileRule.maxSizeInBytes + 1));

    public static TheoryData<string, byte[], long> Rechazos => new()
    {
        { "setup.exe", Pdf, OneKb },
        { "vacio.pdf", [], 0 },
        { "enorme.pdf", Pdf, DocumentFileRule.maxSizeInBytes + 1 },
        { "renombrado.pdf", Exe, OneKb },
    };

    [Theory]
    [MemberData(nameof(Rechazos))]
    public void EnsureValid_CualquierRechazo_ElMensajeNombraElArchivo(string fileName, byte[] header, long size)
    {
        // en el aviso de arribo se suben hasta 5 archivos: el usuario debe saber cuál falló
        var ex = Should.Throw<DomainException>(() => Validate(fileName, header, size));

        ex.Message.ShouldContain($"\"{fileName}\"");
    }

    // ---------------------------------------------------------------- Content-Type para el bucket

    [Theory]
    [InlineData("factura.pdf", "application/pdf")]
    [InlineData("cfdi.xml", "application/xml")]
    [InlineData("lista.xls", "application/vnd.ms-excel")]
    [InlineData("lista.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("FACTURA.PDF", "application/pdf")]
    [InlineData("setup.exe", "application/octet-stream")]
    [InlineData("factura", "application/octet-stream")]
    public void ContentTypeFor_RegresaElTipoSegunLaExtension(string fileName, string expected) =>
        DocumentFileRule.ContentTypeFor(fileName).ShouldBe(expected);

    [Theory]
    [InlineData("factura.PDF", DocumentPreviewKindEnum.Pdf)]
    [InlineData("cfdi.xml", DocumentPreviewKindEnum.Text)]
    [InlineData("lista.xls", DocumentPreviewKindEnum.Spreadsheet)]
    [InlineData("lista.xlsx", DocumentPreviewKindEnum.Spreadsheet)]
    [InlineData("setup.exe", DocumentPreviewKindEnum.None)]
    public void PreviewKindFor_RegresaComoSeMuestraSegunLaExtension(string fileName, DocumentPreviewKindEnum expected) =>
        DocumentFileRule.PreviewKindFor(fileName).ShouldBe(expected);
}
