# Pruebas por capa: qué va dónde y plantillas

## Contenido
1. Estructura de `test/`
2. Ayudantes que ya existen (reúsalos)
3. Domain — plantilla
4. Application — plantilla
5. Infraestructure: repositorio con PostgreSQL — plantilla
6. Infraestructure: cliente HTTP externo — plantilla
7. Presentation — plantilla

---

## 1. Estructura de `test/`

```
test/
├── Directory.Build.props              net10.0, xunit.v3, Shouldly, NSubstitute y sus using globales
├── SIDAPA.domain.test/                → SIDAPA.domain
│   └── Rules/<Module>/…RuleTests.cs
├── SIDAPA.application.test/           → SIDAPA.application
│   ├── Fakes/                         fakes compartidos (repos, bucket, reloj)
│   └── Services/<Module>/<Submodule>/…ServiceTests.cs
├── SIDAPA.infraestructure.test/       → SIDAPA.infraestructure (+ Testcontainers.PostgreSql)
│   ├── Persistence/                   PostgresFixture, TestData
│   ├── Repositories/<Module>/<Submodule>/…RepositoryTests.cs
│   ├── External/<Proveedor>/…Tests.cs
│   └── Security/…Tests.cs
└── SIDAPA.presentation.test/          → SIDAPA.presentation (+ Mvc.Testing)
    ├── SidapaWebFactory.cs
    └── Security/ · Controllers/<Module>/<Submodule>/ · Filters/
```

Cada proyecto referencia **solo** su capa (las inferiores llegan transitivamente). Un proyecto de
pruebas de application no referencia infraestructura: si lo necesitas, la prueba está en la capa equivocada.

La carpeta sigue la ruta **destino** del código, aunque el código de producción todavía no se haya
movido: `ArrivalNoticeService` hoy vive en `Services/Core/`, pero su prueba va en `Services/CaseFile/ArrivalNotice/`.

| Módulo | Submódulos | Ejemplos |
|---|---|---|
| `Core` (no depende de ningún módulo) | `Auth`, `Users`, `Permissions`, `Notifications`, `Storage`, `Home` | `NotificationService`, `UserRepository`, `JwtTokenService` |
| `CaseFile` | `ArrivalNotice`, `Referencias`, `Documents`, `Expediente` | `DocumentService`, `ReferenciaRepository`, `ExpedienteRule` |
| `CustomDeclaration` | `Catalogs`, `Pedimentos`, `Claves`, `Invoices`, `Products` | `PedimentoUrgencyRule`, `CatalogRepository`, `ClaveService` |

Reglas de dominio, enums y excepciones llevan solo módulo (`Rules/CaseFile/`), sin submódulo. Los adaptadores
externos van por proveedor (`External/TaxScan/`, `External/CloudStorage/`) y la seguridad en `Security/`.
Si `docs/refactor-arquitectura-modular.md` existe, es la fuente de verdad y manda sobre esta tabla.

## 2. Ayudantes que ya existen

| Ayudante | Dónde | Para qué |
|---|---|---|
| `FakeReferenciaRepository` | `application.test/Fakes` | Referencias + bitácora en memoria; `Seed`, `Added`, `Deleted`, `Changes`, `SimulateConcurrentChange`, `MandatoryDocumentTypeIds`; `ChangeStatusAsync` respeta "solo si sigue en `from`" |
| `FakeDocumentRepository` | `application.test/Fakes` | `ope_documento` en memoria; `ReferenciaNumbers`, `Seed(...)`, `Saved`, `FailOnAdd` |
| `FakeCloudStorage` | `application.test/Fakes` | Bucket en memoria con bytes completos; `Uploaded`, `ContentTypes`, `Deleted`, `SignedUrls` |
| `FixedTimeProvider` | `application.test/Fakes` | Reloj fijo (`Default` = 2026-11-23 15:30 UTC), `Advance(...)` |
| `PostgresFixture` + `[Collection(PostgresCollection.Name)]` | `infraestructure.test/Persistence` | PostgreSQL 16 con migraciones y roles; `CreateContext()` |
| `TestData` | `infraestructure.test/Persistence` | `UserAsync`, `ClientAsync`, `ReferenciaAsync` con datos únicos |
| `FakeStorageClient`, `FakeBlobSigner` | `infraestructure.test/External/CloudStorage` | SDK de Google sin red |
| `SidapaWebFactory` | `presentation.test` | App completa en memoria; `CreateAnonymousClient()`, `CreateClientAs(rol)`, `CreateToken(...)`, substitutes `Storage`, `TaxScan`, `Email` |

Si un fake nuevo lo van a usar dos clases de prueba, va en `Fakes/`. Si solo lo usa una, puede ser
una clase `private sealed` dentro de esa prueba. Los métodos que el SUT no debe llamar lanzan
`NotSupportedException` (así la prueba avisa si el SUT empieza a usarlos).

## 3. Domain

C# puro: sin dobles, sin async, sin I/O. Las reglas son estáticas; se prueban con `[Theory]`.

```csharp
using SIDAPA.domain.Exceptions.CoreException;
using SIDAPA.domain.Rules.Core;

namespace SIDAPA.domain.test.Rules.Core;

/// <summary>Reglas del mensaje de un aviso (SCRUM-59).</summary>
public class NotificationRuleTests
{
    [Theory]
    [InlineData("Hola", "Hola")]
    [InlineData("  Hola  ", "Hola")]          // se recorta
    public void EnsureValidMessage_Valido_RegresaElTextoRecortado(string message, string expected) =>
        NotificationRule.EnsureValidMessage(message).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValidMessage_Vacio_Lanza(string? message) =>
        Should.Throw<EmptyNotificationMessageException>(() => NotificationRule.EnsureValidMessage(message));

    [Fact]
    public void EnsureValidMessage_EnElLimite_Pasa() =>
        NotificationRule.EnsureValidMessage(new string('x', NotificationRule.MaxMessageLength)).Length
            .ShouldBe(NotificationRule.MaxMessageLength);

    [Fact]
    public void EnsureValidMessage_UnoMasDelLimite_Lanza() =>
        Should.Throw<NotificationMessageTooLongException>(() =>
            NotificationRule.EnsureValidMessage(new string('x', NotificationRule.MaxMessageLength + 1)));
}
```

## 4. Application

El servicio real con sus dependencias falsas. Campos de instancia: xUnit crea una instancia de la clase
por prueba, así que cada prueba arranca con fakes limpios.

```csharp
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;

namespace SIDAPA.application.test.Services.Core.Notifications;

/// <summary>Pruebas de <see cref="NotificationService"/> (SCRUM-59).</summary>
public class NotificationServiceTests
{
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly NotificationService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public NotificationServiceTests() => _service = new NotificationService(_notifications, _users, _email);

    [Fact]
    public async Task MarkAsReadAsync_AvisoDeOtroUsuario_Lanza()
    {
        _notifications.MarkAsReadAsync(1, 99, Arg.Any<CancellationToken>()).Returns(false);

        await Should.ThrowAsync<NotificationNotFoundException>(() => _service.MarkAsReadAsync(1, 99, _ct));
    }
}
```

Patrones útiles:
- Capturar lo que se guardó: `await repo.AddAsync(Arg.Do<NotificationEntity>(n => saved = n), Arg.Any<CancellationToken>());`
  o un fake con lista `Saved`.
- Verificar que **no** pasó algo: `await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);`
- Simular fallo: `repo.AddAsync(...).ThrowsAsync(new InvalidOperationException("BD caída"));`
  (`using NSubstitute.ExceptionExtensions;`).
- Archivos: bytes reales con firma correcta (`"%PDF-1.4…"u8.ToArray()`, ZIP `50 4B 03 04`, OLE `D0 CF 11 E0…`)
  y el `DocumentFileValidator` real; el tamaño se puede declarar mayor que los bytes (`size: 20 MB + 1`)
  para probar el límite sin reservar memoria.

## 5. Infraestructure: repositorio con PostgreSQL

```csharp
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.test.Persistence;

namespace SIDAPA.infraestructure.test.Repositories.Core.Notifications;

[Collection(PostgresCollection.Name)]
public class NotificationRepositoryTests(PostgresFixture db)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task CountUnreadAsync_SoloCuentaLosDelUsuario()
    {
        await using var arrange = db.CreateContext();          // omite la prueba si no hay Docker
        var user = await TestData.UserAsync(arrange);
        var other = await TestData.UserAsync(arrange);
        // … insertar avisos de ambos …

        await using var context = db.CreateContext();          // contexto limpio para el SUT
        var count = await new NotificationRepository(context).CountUnreadAsync(user.Id, _ct);

        count.ShouldBe(2);
    }
}
```

- Un contexto para preparar datos y **otro** para el repositorio (y otro para verificar): así no lees
  entidades que EF tiene en caché y pruebas lo que de verdad quedó en la base.
- Nunca asumas tablas vacías: filtra por los ids que tú creaste.
- Fechas a guardar: `DateTimeKind.Unspecified`.

## 6. Infraestructure: cliente HTTP externo

`TaxScanClient` recibe un `HttpClient`: dale un handler falso y prueba el contrato (ruta, multipart,
snake_case → DTO, errores HTTP).

```csharp
private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Request = request;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

var client = new TaxScanClient(new HttpClient(handler) { BaseAddress = new Uri("http://reader.test/") });
```

## 7. Presentation

Comparte la factory por clase (`IClassFixture<SidapaWebFactory>`); el host tarda en arrancar.

```csharp
namespace SIDAPA.presentation.test.Controllers.CustomDeclaration.Claves;

public class ClavesControllerTests(SidapaWebFactory factory) : IClassFixture<SidapaWebFactory>
{
    [Fact]
    public async Task Index_Capturador_VaAAccesoDenegado()
    {
        var response = await factory.CreateClientAs(RolesEnum.CAPTURADOR)
            .GetAsync("/Claves", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.ShouldBe("/Auth/Denied");
    }
}
```

- Las pantallas que leen la BD necesitan que sustituyas el **servicio de application** que usan
  (`services.RemoveAll<IClaveService>(); services.AddSingleton(sub);` con
  `factory.WithWebHostBuilder(b => b.ConfigureTestServices(...))`), no la BD.
- Para políticas puras usa `IAuthorizationService` de `factory.Services` (más rápido que HTTP).
- POST con antiforgery: primero GET de la página, toma el token del HTML y la cookie
  `.AspNetCore.Antiforgery.*`, luego POST con ambos.
