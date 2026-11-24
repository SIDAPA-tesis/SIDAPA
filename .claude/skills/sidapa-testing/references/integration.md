# Pruebas de integración: Testcontainers y WebApplicationFactory

## PostgreSQL real con Testcontainers (`SIDAPA.infraestructure.test`)

### Por qué no EF InMemory ni SQLite
Los repositorios usan cosas que solo PostgreSQL tiene o que cambian de comportamiento fuera de él:
esquema `sidapa`, la secuencia del número de referencia (`NextNumberAsync`), `ExecuteUpdateAsync`,
transacciones, índices únicos con nombre, `timestamp without time zone`, `char(3)` de la aduana. Una
prueba en InMemory pasaría con código que en producción truena.

### Cómo funciona `PostgresFixture`
- Arranca `postgres:16-alpine` (la misma imagen que `docker-compose.yml`) **una vez por corrida**,
  aplica **todas las migraciones reales** (así también se prueban) y siembra `cat_rol`.
- Catálogos sembrados por migraciones (`HasData`): estatus, tipos de documento, tipos de operación,
  aduanas (430 Veracruz, 470 AICM…). Úsalos tal cual.
- Se comparte entre clases con `[Collection(PostgresCollection.Name)]`. Las clases de la misma colección
  no corren en paralelo entre sí.
- `CreateContext()` regresa un `SidapaDbContext` nuevo apuntando al contenedor. Llama primero a
  `EnsureAvailable()`: sin Docker la prueba se **omite**; con `SIDAPA_REQUIRE_DOCKER=1` **falla**.

### Reglas para que las pruebas no se pisen
- Cada prueba crea sus datos con `TestData` (correos, RFC y números de referencia únicos).
- Filtra siempre por los ids que creaste; nunca `CountAsync()` sobre toda la tabla.
- Usa un contexto para preparar, otro para el SUT y otro para verificar.
- No borres ni trunques tablas: otras pruebas pueden estar usándolas.
- La secuencia de referencias es global: afirma que los números son **distintos y crecientes**, no un valor exacto.

### Requisitos
- Docker Desktop encendido (Windows) o Docker en Linux/CI.
- La primera corrida descarga la imagen (~100 MB).

## App completa en memoria con WebApplicationFactory (`SIDAPA.presentation.test`)

### Qué hace `SidapaWebFactory`
- Usa el `Program.cs` real: mismo pipeline de autenticación (JWT en cookie `sidapa_token`), renovación
  (`TokenRenewalMiddleware`), políticas (`AddSidapaAuthorization`), filtros y rutas MVC.
- Entorno `Testing`: no lee user-secrets ni crea usuarios de prueba.
- Configuración falsa mínima para que `Program.cs` arranque: `Jwt:Key` de pruebas (`SidapaWebFactory.JwtKey`),
  cadena de conexión a un puerto cerrado con `Timeout=1` (el seeder falla rápido y solo deja un warning),
  `TaxScan:BaseUrl` ficticio y un service account de Google generado en memoria.
- Sustituye `ICloudStorageService`, `ITaxScanClient` e `IEmailSender` por NSubstitute
  (`factory.Storage`, `factory.TaxScan`, `factory.Email`).

### Clientes
- `CreateAnonymousClient()`: sin sesión, **sin seguir redirecciones** (así afirmas `Location`).
- `CreateClientAs(RolesEnum.GLOSADOR, userId: 5, clientId: null)`: con cookie firmada por la app.
- `CreateToken(...)`: el `AuthToken` por si necesitas armar la cookie a mano.

### Pantallas que leen la base
La BD de la factory no existe. Si la acción llama a un servicio que lee la BD, sustituye ese servicio:

```csharp
var claves = Substitute.For<IClaveService>();
claves.ObtenerTableroAsync(Arg.Any<FiltroClaves>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
    .Returns(new TableroClavesDto(/* … */));

var client = factory
    .WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<IClaveService>(); s.AddSingleton(claves); }))
    .CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
```

`WithWebHostBuilder` crea otro host: úsalo con moderación (cuesta arrancarlo).

### Qué sí probar en presentation
- Quién entra a qué (políticas) y a dónde redirige sin sesión / sin permiso.
- Que el token se lee de la cookie, que uno vencido o falsificado no da sesión y que se renueva a media vida.
- Que una `DomainException` en un POST re-renderiza la misma vista con el mensaje (`DomainExceptionFilter`).
- Que una ruta responde con la vista y el código correctos.

### Qué no probar en presentation
Reglas de negocio (20 MB, estatus, folios): ya están probadas en domain/application. Repetirlas aquí
solo hace las pruebas lentas y frágiles.

## Servicios externos reales (GCS, reader)
Las pruebas automáticas no llaman a Google Cloud ni al reader real. Si hace falta una prueba de humo
contra el servicio real, márcala para que se **omita** si no hay credenciales:

```csharp
var bucket = Environment.GetEnvironmentVariable("SIDAPA_GCS_TEST_BUCKET");
Assert.SkipWhen(string.IsNullOrEmpty(bucket), "Define SIDAPA_GCS_TEST_BUCKET y GOOGLE_APPLICATION_CREDENTIALS para correrla.");
```
