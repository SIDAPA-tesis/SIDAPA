using System.Net;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Rules.Core;

public static class DocumentFileRule
{
    public const long maxSizeInBytes = 20L * 1024 * 1024;

    public const int HeaderLength = 8;
    
    public static readonly IReadOnlyList<String> AllowedExtensions = [".pdf", ".xml", ".xls", ".xlsx"];

    // Primeros bytes que identifican el tipo de archivo.

    private static ReadOnlySpan<byte> PdfSignature => "%PDF-"u8;
    private static ReadOnlySpan<byte> ZipSignature => new byte[] { 0x50, 0x4B, 0x03, 0x04};
    private static ReadOnlySpan<byte> OleSignature => new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1};
    private static ReadOnlySpan<byte> Uft8Bom => new byte[] { 0xEF, 0xBB, 0xBF};
    private static ReadOnlySpan<byte> Whitespace => " \t\r\n"u8;

    public static void EnsureValid(string fileName, long sizeInBytes, ReadOnlySpan<byte> header)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if(!AllowedExtensions.Contains(extension)) throw new FileTypeNotAllowedException(fileName);

        if(sizeInBytes == 0) throw new EmptyFileException(fileName);

        if(sizeInBytes > maxSizeInBytes) throw new FileTooLargeException(fileName, sizeInBytes, maxSizeInBytes);

        if(!ContentMatches(extension, header)) throw new FileContentMismatchException(fileName);
    }

    private static bool ContentMatches(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".pdf" => header.StartsWith(PdfSignature),
        ".xlsx" => header.StartsWith(ZipSignature),
        ".xls" => header.StartsWith(OleSignature),
        ".xml" => LooksLikeXml(header),
        _ => false
    };

    private static bool LooksLikeXml(ReadOnlySpan<byte> header)
    {
        if(header.StartsWith(Uft8Bom)) header = header[Uft8Bom.Length..];

        header = header.TrimStart(Whitespace);
        return header.Length > 0 && header[0] == (byte)'<';
    }
}