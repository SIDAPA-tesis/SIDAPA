namespace SIDAPA.application.Interface.Core;

public interface IDocumentFileValidator
{
    Task ValidateAsync(string fileName, long sizeInBytes, Stream content, CancellationToken ct = default);
}