using System.Globalization;
using System.Text;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.domain.Rules.Core;

/// <summary>
/// Nombre normalizado del archivo dentro del bucket:
/// <c>referencias/{REFERENCIA}/{TIPO_DOCUMENTO}/{yyyyMMdd-HHmmss}_{id}{.ext}</c>
/// El nombre original del cliente solo se guarda en la BD (nombre_archivo), nunca en la ruta.
/// </summary>
public static class DocumentPathRule
{
    public const string RootFolder = "referencias";

    /// <summary>Largo de la columna nombre_archivo.</summary>
    public const int MaxFileNameLength = 200;

    public static string Build(
        string referenciaNumber, DocumentTypeEnum documentType, string fileName, DateTime uploadedAt, Guid uniqueId)
    {
        var folder = SanitizeReferencia(referenciaNumber);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = uploadedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var shortId = uniqueId.ToString("N")[..8];   // evita que dos archivos del mismo segundo se pisen

        return $"{RootFolder}/{folder}/{documentType}/{timestamp}_{shortId}{extension}";
    }

    /// <summary>Nombre original sin la ruta que mandan algunos navegadores y recortado sin perder la extensión.</summary>
    public static string OriginalFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
        if (name.Length <= MaxFileNameLength)
            return name;

        var extension = Path.GetExtension(name);
        return name[..(MaxFileNameLength - extension.Length)] + extension;
    }

    // La referencia la genera el sistema, pero se limpia por si acaso: solo A-Z, 0-9 y guion.
    private static string SanitizeReferencia(string referenciaNumber)
    {
        var clean = new StringBuilder();
        foreach (var c in referenciaNumber.Trim().ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '-')
                clean.Append(c);
        }

        if (clean.Length == 0)
            throw new ArgumentException("El número de referencia no tiene caracteres válidos.", nameof(referenciaNumber));

        return clean.ToString();
    }
}