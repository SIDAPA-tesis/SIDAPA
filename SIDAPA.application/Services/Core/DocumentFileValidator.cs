using SIDAPA.application.Interface.Core;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.application.Services.Core;

public class DocumentFileValidator : IDocumentFileValidator
{
    public async Task ValidateAsync(string fileName, long sizeInBytes, Stream content, CancellationToken ct = default)
    {
        var header = new byte[DocumentFileRule.HeaderLength];
        var read = await content.ReadAtLeastAsync(header , header.Length, throwOnEndOfStream: false, ct);

        DocumentFileRule.EnsureValid(fileName, sizeInBytes, header.AsSpan(0, read));
    }
}