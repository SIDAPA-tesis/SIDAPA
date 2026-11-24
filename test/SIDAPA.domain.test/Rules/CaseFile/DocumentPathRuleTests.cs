using SIDAPA.domain.Enums.Core;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.CaseFile;

/// <summary>Pruebas del nombre normalizado de los archivos en el bucket (SCRUM-48).</summary>
public class DocumentPathRuleTests
{
    private static readonly DateTime UploadedAt = new(2026, 11, 12, 6, 36, 38);
    private static readonly Guid Id = Guid.Parse("4a1217de-bafa-4b45-980d-6329bb4bdb36");

    [Fact]
    public void Build_ArmaLaRutaNormalizada()
    {
        var path = DocumentPathRule.Build("R-000123", DocumentTypeEnum.LISTA_EMPAQUE, "Mi Lista FINAL.PDF", UploadedAt, Id);

        path.ShouldBe("referencias/R-000123/LISTA_EMPAQUE/20261112-063638_4a1217de.pdf");
    }

    [Fact]
    public void Build_LimpiaLaReferencia()
    {
        var path = DocumentPathRule.Build(" r-00 01/../ ", DocumentTypeEnum.DOCUMENTO_TRANSPORTE, "bl.xml", UploadedAt, Id);

        path.ShouldStartWith("referencias/R-0001/DOCUMENTO_TRANSPORTE/");
    }

    [Fact]
    public void Build_DosArchivosDelMismoSegundoNoSePisan()
    {
        var first = DocumentPathRule.Build("R-1", DocumentTypeEnum.LISTA_EMPAQUE, "a.pdf", UploadedAt, Guid.NewGuid());
        var second = DocumentPathRule.Build("R-1", DocumentTypeEnum.LISTA_EMPAQUE, "a.pdf", UploadedAt, Guid.NewGuid());

        first.ShouldNotBe(second);
    }

    [Fact]
    public void OriginalFileName_QuitaLaRutaDelNavegador()
    {
        DocumentPathRule.OriginalFileName(@"C:\fakepath\factura.pdf").ShouldBe("factura.pdf");
    }

    [Fact]
    public void OriginalFileName_RecortaSinPerderLaExtension()
    {
        var name = DocumentPathRule.OriginalFileName(new string('a', 250) + ".xlsx");

        name.Length.ShouldBe(DocumentPathRule.MaxFileNameLength);
        name.ShouldEndWith(".xlsx");
    }
}
