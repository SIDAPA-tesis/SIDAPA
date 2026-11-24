# Refactor para testabilidad (catálogo de seams de SIDAPA)

Un *seam* es un punto donde puedes cambiar lo que hace el código sin editarlo: normalmente una
dependencia que entra por el constructor. Haz el cambio más pequeño que te permita probar, en este
orden: **caracteriza → refactoriza → verifica que sigue en verde → prueba lo nuevo**.

## Pruebas de caracterización
Antes de refactorizar código sin pruebas, escribe pruebas que describan lo que el código hace **hoy**,
aunque parezca raro. Si durante el refactor una falla, cambiaste comportamiento. Si lo que hace hoy es un
bug, la prueba lo documenta (nómbrala así: `..._HoyGuardaLaHoraLocal_Bug`) y lo reportas; el arreglo va
en un paso aparte con su propia prueba.

## 1. Reloj: `DateTime.UtcNow` / `DateTime.Now` → `TimeProvider`
Síntoma: el servicio llama `DateTime.UtcNow` y no puedes afirmar la fecha guardada.

`AddApplication()` ya registra `services.TryAddSingleton(TimeProvider.System)`. Solo hay que pedirlo:

```csharp
// Antes
public class NotificationService(INotificationRepository notifications, IUserRepository users, IEmailSender emailSender)
{
    private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}

// Después
public class NotificationService(INotificationRepository notifications, IUserRepository users,
    IEmailSender emailSender, TimeProvider clock)
{
    // UTC sin zona: la columna es "timestamp without time zone".
    private DateTime UtcNow() => DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);
}
```

- Si el servicio se registra en infraestructura (`AddInfraestructure`) también lo resuelve el contenedor:
  `TimeProvider` ya es singleton global.
- Actualiza **todas** las construcciones manuales (`new NotificationService(...)`) en pruebas.
- En pruebas: `new FixedTimeProvider()` (application.test/Fakes).
- `ArrivalNoticeService` ya sigue este patrón: úsalo como referencia.

## 2. Identificadores aleatorios: `Guid.NewGuid()`
Si el valor solo debe ser **único**, no lo inyectes: prueba la propiedad (dos llamadas → rutas distintas),
como `DocumentPathRuleTests`. Si necesitas el valor exacto, la regla de dominio ya lo recibe como parámetro
(`DocumentPathRule.Build(..., Guid id)`): prueba la regla con un Guid fijo.

## 3. Lógica de negocio en un controller o ViewComponent
Síntoma: un `if` de negocio, un cálculo o una consulta en `Controllers/` o `ViewComponents/`.
Mueve la lógica a un servicio de application (interfaz en `Interface/`, implementación en `Services/`,
registro en `AddApplication`) y deja el controller en: leer input → llamar servicio → elegir vista.
Las pruebas del comportamiento van en application.test; en presentation.test solo lo de HTTP.

## 4. `SidapaDbContext` fuera de infraestructura
Síntoma: un ViewComponent o controller inyecta `SidapaDbContext` (hallazgo H6 del refactor modular).
Crea un repositorio (interfaz en application, implementación en infraestructure) y un servicio que lo use.
Prueba el repositorio con `PostgresFixture` y el servicio con un fake.

## 5. Dependencia concreta creada con `new`
Síntoma: `new SmtpClient(...)`, `new HttpClient()`, `StorageClient.Create()` dentro de un método.
Extrae una interfaz en application (si es un puerto del negocio) o recibe la dependencia por constructor
(si es detalle de infraestructura) y regístrala en DI. Para HTTP usa un typed client (`AddHttpClient<I, T>`),
así la prueba pasa un `HttpMessageHandler` falso.

## 6. Clase sellada o estática de un SDK
`StorageClient` de Google es abstracto con métodos virtuales (se hereda un fake) y `UrlSigner` acepta un
`IBlobSigner` propio. Si un SDK no deja nada de eso, envuélvelo en un adaptador delgado propio y prueba
el adaptador con integración, no con unit tests.

## 7. Métodos privados "difíciles de probar"
No los vuelvas públicos ni uses `InternalsVisibleTo` para probarlos. Pruébalos a través del método
público que los usa. Si la lógica es lo bastante importante para probarla sola, probablemente es una
**regla de dominio**: muévela a `SIDAPA.domain/Rules/` como función pura y pruébala ahí.

## 8. Excepciones genéricas
Si el código lanza `InvalidOperationException("...")` para un error de negocio, la prueba tiene que
comparar textos. La convención del repo es una excepción por error que hereda de `DomainException`
(`SIDAPA.domain/Exceptions/`). Crear la excepción es un cambio de comportamiento visible (el
`DomainExceptionFilter` la mostrará al usuario): hazlo con su prueba y dilo en el reporte.

## Límites del refactor
- No cambies firmas públicas que usen otras capas sin actualizar todas las llamadas y correr `dotnet build SIDAPA.sln`.
- No cambies nombres de tabla/columna ni migraciones para "facilitar" una prueba.
- No agregues NuGet ni proyectos sin preguntar.
- Un commit (o paso) para el refactor, otro para las pruebas nuevas / el arreglo del bug.
