using System.Text;
using SIDAPA.application.Services.Core;
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.test.Services.CaseFile.Documents;

/// <summary>
/// Pruebas de <see cref="DocumentFileValidator"/> (SCRUM-50): lee el encabezado del Stream y aplica
/// <see cref="DocumentFileRule"/>. Las particiones de tipo, tamaño y firma se prueban a fondo en
/// <c>DocumentFileRuleTests</c> (domain); aquí se prueba lo que agrega el validador: leer bien el Stream
/// (corto, vacío, que llega en pedazos) y usar el tamaño declarado.
/// </summary>
public class DocumentFileValidatorTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\nfactura de prueba\n%%EOF");
    private static readonly byte[] Xls = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00, 0x00];
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00];

    private readonly DocumentFileValidator _validator = new();
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private Task ValidateAsync(string fileName, byte[] bytes, long? size = null) =>
        _validator.ValidateAsync(fileName, size ?? bytes.Length, new MemoryStream(bytes), _ct);

    [Fact]
    public async Task ValidateAsync_PdfValido_Pasa()
    {
        await Should.NotThrowAsync(() => ValidateAsync("factura.pdf", Pdf));
    }

    [Fact]
    public async Task ValidateAsync_ExeRenombradoAPdf_LanzaContenidoNoCorresponde()
    {
        await Should.ThrowAsync<FileContentMismatchException>(() => ValidateAsync("bl.pdf", Exe));
    }

    [Fact]
    public async Task ValidateAsync_ExtensionNoPermitida_LanzaTipoNoPermitido()
    {
        await Should.ThrowAsync<FileTypeNotAllowedException>(() => ValidateAsync("setup.exe", Exe));
    }

    [Fact]
    public async Task ValidateAsync_StreamMasCortoQueElEncabezado_NoFallaPorFinDeStream()
    {
        // 5 bytes < HeaderLength (8): no debe lanzar EndOfStreamException, solo evaluar lo que hay
        var mini = Encoding.ASCII.GetBytes("%PDF-");

        await Should.NotThrowAsync(() => ValidateAsync("mini.pdf", mini));
    }

    [Fact]
    public async Task ValidateAsync_StreamVacio_LanzaArchivoVacio()
    {
        await Should.ThrowAsync<EmptyFileException>(() => ValidateAsync("vacio.pdf", []));
    }

    [Fact]
    public async Task ValidateAsync_TamanoDeclaradoPeroStreamVacio_LanzaContenidoNoCorresponde()
    {
        // la subida se cortó: el navegador declaró 1 KB pero no llegó ningún byte
        await Should.ThrowAsync<FileContentMismatchException>(() => ValidateAsync("factura.pdf", [], size: 1024));
    }

    [Fact]
    public async Task ValidateAsync_StreamQueLlegaDeAUnByte_LeeElEncabezadoCompleto()
    {
        // la firma OLE de .xls ocupa los 8 bytes; un Stream de red puede entregarlos en varias lecturas
        var stream = new TricklingStream(Xls);

        await Should.NotThrowAsync(() => _validator.ValidateAsync("lista.xls", Xls.Length, stream, _ct));
    }

    [Fact]
    public async Task ValidateAsync_TamanoDeclaradoEnElLimite_Pasa()
    {
        // límite exacto: 20 MB sí pasa. El tamaño se declara; no hace falta reservar 20 MB de memoria
        await Should.NotThrowAsync(() => ValidateAsync("factura.pdf", Pdf, size: DocumentFileRule.maxSizeInBytes));
    }

    [Fact]
    public async Task ValidateAsync_TamanoDeclaradoUnByteSobreElLimite_LanzaArchivoMuyGrande()
    {
        // el validador confía en el tamaño declarado (IFormFile.Length), no en cuántos bytes leyó
        await Should.ThrowAsync<FileTooLargeException>(() =>
            ValidateAsync("factura.pdf", Pdf, size: DocumentFileRule.maxSizeInBytes + 1));
    }

    [Fact]
    public async Task ValidateAsync_TokenCancelado_NoValida()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            _validator.ValidateAsync("factura.pdf", Pdf.Length, new MemoryStream(Pdf), cts.Token));
    }

    /// <summary>Stream que entrega un byte por lectura, como una subida que llega por partes.</summary>
    private sealed class TricklingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            base.ReadAsync(buffer, offset, Math.Min(count, 1), ct);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], ct);
    }
}
