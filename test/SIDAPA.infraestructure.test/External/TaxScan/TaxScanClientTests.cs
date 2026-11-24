using System.Net;
using System.Text;
using SIDAPA.infraestructure.ApiClients;

namespace SIDAPA.infraestructure.test.External.TaxScan;

/// <summary>
/// Contrato con sidapa-reader (POST /invoices/read, multipart, JSON snake_case) probado con un
/// <see cref="HttpMessageHandler"/> falso: sin red ni lector real. Reemplaza a DevTaxScanController.
/// </summary>
public class TaxScanClientTests
{
    private const string ReaderJson = """
        {
          "read_source": "XML",
          "invoice_number": "F-1001",
          "invoice_date": "2026-11-20",
          "currency": "USD",
          "incoterm": "FOB",
          "supplier": { "name": "ACME Corp", "tax_id": "US123", "address": "Main St 1", "country": "USA" },
          "total": 1500.50,
          "lines": [
            { "part_number": "P-1", "description": "Tornillo", "quantity": 100, "unit": "PZA",
              "unit_price": 15.005, "amount": 1500.50, "origin_country": "CHN", "confidence": 0.97 }
          ],
          "warnings": ["no se encontró el incoterm en la línea 2"]
        }
        """;

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static (TaxScanClient Client, StubHandler Handler) Create(HttpStatusCode status = HttpStatusCode.OK, string body = ReaderJson)
    {
        var handler = new StubHandler(status, body);
        return (new TaxScanClient(new HttpClient(handler) { BaseAddress = new Uri("http://reader.test/") }), handler);
    }

    private static MemoryStream File() => new(Encoding.ASCII.GetBytes("%PDF-1.4 factura"));

    [Fact]
    public async Task ReadInvoiceAsync_MapeaElJsonSnakeCaseAlDto()
    {
        var (client, _) = Create();

        var result = await client.ReadInvoiceAsync(File(), "factura.pdf", _ct);

        result.ReadSource.ShouldBe("XML");
        result.InvoiceNumber.ShouldBe("F-1001");
        result.InvoiceDate.ShouldBe(new DateOnly(2026, 11, 20));
        result.Currency.ShouldBe("USD");
        result.Supplier.ShouldNotBeNull().TaxId.ShouldBe("US123");
        result.Total.ShouldBe(1500.50m);
        var line = result.Lines.ShouldHaveSingleItem();
        line.PartNumber.ShouldBe("P-1");
        line.UnitPrice.ShouldBe(15.005m);
        line.OriginCountry.ShouldBe("CHN");
        result.Warnings.ShouldHaveSingleItem().ShouldContain("incoterm");
    }

    [Fact]
    public async Task ReadInvoiceAsync_PosteaMultipartAInvoicesRead()
    {
        var (client, handler) = Create();

        await client.ReadInvoiceAsync(File(), "factura.pdf", _ct);

        handler.Request.ShouldNotBeNull();
        handler.Request.Method.ShouldBe(HttpMethod.Post);
        handler.Request.RequestUri!.AbsoluteUri.ShouldBe("http://reader.test/invoices/read");
        handler.ContentType.ShouldStartWith("multipart/form-data");
        handler.Body.ShouldContain("name=file");
        handler.Body.ShouldContain("filename=factura.pdf");
    }

    [Theory]
    [InlineData("factura.pdf", "application/pdf")]
    [InlineData("cfdi.XML", "application/xml")]
    [InlineData("lista.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("lista.xls", "application/vnd.ms-excel")]
    [InlineData("foto.jpg", "application/octet-stream")]
    public async Task ReadInvoiceAsync_MandaElContentTypeSegunLaExtension(string fileName, string expected)
    {
        var (client, handler) = Create();

        await client.ReadInvoiceAsync(File(), fileName, _ct);

        handler.Body.ShouldContain($"Content-Type: {expected}");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReadInvoiceAsync_ErrorDelLector_LanzaConCodigoYArchivo(HttpStatusCode status)
    {
        var (client, _) = Create(status, """{"detail":"archivo ilegible"}""");

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.ReadInvoiceAsync(File(), "factura.pdf", _ct));

        ex.Message.ShouldContain(((int)status).ToString());
        ex.Message.ShouldContain("factura.pdf");
        ex.Message.ShouldContain("archivo ilegible");
    }

    [Fact]
    public async Task ReadInvoiceAsync_RespuestaNull_Lanza()
    {
        var (client, _) = Create(body: "null");

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.ReadInvoiceAsync(File(), "factura.pdf", _ct));

        ex.Message.ShouldContain("empty response");
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = "";
        public string ContentType { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            if (request.Content is not null)
            {
                ContentType = request.Content.Headers.ContentType?.ToString() ?? "";
                Body = await request.Content.ReadAsStringAsync(ct);
            }
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
