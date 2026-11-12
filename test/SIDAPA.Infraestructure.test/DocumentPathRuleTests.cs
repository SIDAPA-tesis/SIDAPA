using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;
using Xunit;

namespace SIDAPA.Infraestructure.test;

/// <summary>Pruebas del nombre normalizado de los archivos en el bucket (SCRUM-48).</summary>
public class DocumentPathRuleTests
{
    private static readonly DateTime UploadedAt = new(2026, 11, 12, 6, 36, 38);
    private static readonly Guid Id = Guid.Parse("4a1217de-bafa-4b45-980d-6329bb4bdb36");

    [Fact]
    public void Build_ArmaLaRutaNormalizada()
    {
        var path = DocumentPathRule.Build("R-000123", DocumentTypeEnum.LISTA_EMPAQUE, "Mi Lista FINAL.PDF", UploadedAt, Id);

        Assert.Equal("referencias/R-000123/LISTA_EMPAQUE/20261112-063638_4a1217de.pdf", path);
    }

    [Fact]
    public void Build_LimpiaLaReferencia()
    {
        var path = DocumentPathRule.Build(" r-00 01/../ ", DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "bl.xml", UploadedAt, Id);

        Assert.StartsWith("referencias/R-0001/DOCUMENTO_TRANSPORTE/", path);
    }

    [Fact]
    public void Build_DosArchivosDelMismoSegundoNoSePisan()
    {
        var first = DocumentPathRule.Build("R-1", DocumentTypeEnum.LISTA_EMPAQUE, "a.pdf", UploadedAt, Guid.NewGuid());
        var second = DocumentPathRule.Build("R-1", DocumentTypeEnum.LISTA_EMPAQUE, "a.pdf", UploadedAt, Guid.NewGuid());

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void OriginalFileName_QuitaLaRutaDelNavegador()
    {
        Assert.Equal("factura.pdf", DocumentPathRule.OriginalFileName(@"C:\fakepath\factura.pdf"));
    }

    [Fact]
    public void OriginalFileName_RecortaSinPerderLaExtension()
    {
        var name = DocumentPathRule.OriginalFileName(new string('a', 250) + ".xlsx");

        Assert.Equal(DocumentPathRule.MaxFileNameLength, name.Length);
        Assert.EndsWith(".xlsx", name);
    }
}