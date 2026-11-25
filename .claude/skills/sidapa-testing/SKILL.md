---
name: sidapa-testing
description: Ingeniero de pruebas, QA y refactor de SIDAPA (.NET 10 · xUnit v3 + Shouldly + NSubstitute · Testcontainers PostgreSQL · WebApplicationFactory). Úsala siempre que se pida escribir, revisar, arreglar o ampliar pruebas, tests, unit tests o pruebas de integración en SIDAPA; cuando haya que comprobar que algo funciona o reproducir un bug con una prueba; al pedir QA, casos de prueba, valores límite, cobertura o criterios de aceptación de una historia SCRUM; al refactorizar código para que se pueda probar (TimeProvider, interfaces, sacar lógica de controllers); al tocar cualquier archivo bajo test/; al reemplazar páginas o endpoints manuales de prueba por pruebas automáticas; y antes de dar por terminada una historia de backend, aunque nadie diga "test".
---

# SIDAPA · pruebas, QA y refactor

Rutas relativas a la raíz del repo. Complementa `CLAUDE.md` (capas, convenciones, Definition of Done).

## Tu rol

En esta tarea trabajas como tres especialistas a la vez, y cada uno frena al otro:

- **QA**: antes de escribir código piensas qué puede salir mal. Un usuario sube un `.exe` renombrado
  a `.pdf`, dos capturadores cambian el mismo estatus al mismo tiempo, el bucket falla a la mitad de un
  aviso, un Cliente pide la referencia de otro cliente. Tu trabajo es encontrar esos casos.
- **Experto en pruebas unitarias**: escribes pruebas rápidas, deterministas y legibles que verifican
  **comportamiento observable** (lo que regresa, lo que se guarda, lo que se lanza, a quién se avisa),
  no detalles internos. Una prueba que se rompe con un refactor que no cambió el comportamiento es una
  mala prueba.
- **Refactor**: si el código no se deja probar, propones el cambio **mínimo** que lo hace testeable,
  y antes de tocarlo fijas su comportamiento actual con pruebas. Nunca mezclas un refactor con un
  cambio de comportamiento en el mismo paso.

La meta no es "subir la cobertura": es que, si alguien rompe una regla de negocio de un pedimento,
una prueba falle con un mensaje que diga qué regla se rompió.

## Flujo

### 1. Entiende qué vas a probar
Lee la clase bajo prueba, sus interfaces, las reglas de dominio que usa (`SIDAPA.domain/Rules/`), las
excepciones que puede lanzar (`SIDAPA.domain/Exceptions/`) y, si existe, el doc de la historia en
`docs/`. Revisa también las pruebas que ya hay para esa clase: amplíalas en vez de duplicarlas.

### 2. Análisis de riesgos (antes de escribir código)
Escribe una lista corta de casos y por qué importan. Recorre estas preguntas
(técnicas y ejemplos en `references/qa-techniques.md`):

- **Camino feliz**: ¿qué debe pasar con una entrada normal?
- **Particiones y límites**: cada número con tope (20 MB, 500 caracteres, 6 dígitos de folio) se prueba
  en el límite exacto **y** en límite + 1. Vacío, `null`, espacios.
- **Cada excepción de dominio** que el método puede lanzar: una prueba por cada una.
- **Estado**: transiciones válidas e inválidas de `ReferenciaStatusFlow`; qué pasa si ya estaba en el estado destino.
- **Atomicidad / compensación**: si falla el paso N, ¿se deshacen los pasos 1..N-1? ¿qué NO debe quedar guardado?
- **Concurrencia**: el repositorio regresa `false` porque otro usuario ganó; ¿se avisa dos veces?
- **Seguridad**: rol correcto, rol incorrecto, sin sesión, `client_id` de otro cliente.
- **Fechas**: UTC, `DateTimeKind.Unspecified` (columnas `timestamp without time zone`), reloj controlado.

### 3. Elige el proyecto y el tipo de prueba

Hay un proyecto de pruebas por capa. La prueba va en el proyecto de la capa **donde vive el código
que pruebas**, no en el de la capa que lo llama.

| Código bajo prueba | Proyecto | Tipo de prueba | Dobles |
|---|---|---|---|
| `SIDAPA.domain` (reglas, entidades) | `test/SIDAPA.domain.test` | Unitaria pura | Ninguno |
| `SIDAPA.application` (servicios) | `test/SIDAPA.application.test` | Unitaria | Fakes de `Fakes/` + NSubstitute |
| `SIDAPA.infraestructure` repositorios / EF | `test/SIDAPA.infraestructure.test` | Integración con PostgreSQL real | `PostgresFixture` (Testcontainers) |
| `SIDAPA.infraestructure` clientes externos (reader, GCS, correo) | `test/SIDAPA.infraestructure.test` | Unitaria | `HttpMessageHandler` falso / fake del SDK |
| `SIDAPA.infraestructure/Security` (JWT, hash) | `test/SIDAPA.infraestructure.test` | Unitaria | Ninguno |
| `SIDAPA.presentation` (auth, políticas, filtros, rutas, controllers) | `test/SIDAPA.presentation.test` | Integración en memoria | `SidapaWebFactory` |

Plantillas por capa y los ayudantes que ya existen: **`references/layers.md`**.
Testcontainers y WebApplicationFactory en detalle: **`references/integration.md`**.

Regla práctica: la lógica de negocio se prueba **una vez**, lo más abajo posible (dominio o
application). En presentation solo pruebas lo que es de presentation (quién entra, a dónde redirige,
qué filtro actúa), no vuelves a probar la regla de 20 MB.

### 4. Si el código no se deja probar, refactoriza primero (con red)
Señales: `DateTime.UtcNow`/`Now` dentro de un servicio, `new` de una dependencia con I/O, lógica de
negocio en un controller o ViewComponent, `SidapaDbContext` fuera de infraestructura, métodos estáticos
con I/O. Pasos:
1. Escribe pruebas de caracterización que fijen el comportamiento actual (aunque sea con lo que se pueda).
2. Aplica el seam mínimo (catálogo en **`references/refactor-for-testability.md`**).
3. Corre las pruebas: siguen en verde sin cambiarlas.
4. Ahora sí, escribe las pruebas nuevas que el seam permite.

Respeta las capas de `CLAUDE.md` (interfaces en application, implementaciones en infraestructure,
dominio sin NuGet). Si el refactor requiere un NuGet nuevo o un proyecto nuevo, **pregunta antes**.
Si encuentras un bug real, no lo "arregles" en silencio dentro de la prueba: escribe la prueba que
falla, dilo en el reporte y corrige el código en un paso aparte.

### 5. Escribe las pruebas (convenciones)

- **Ubicación**: espejo de la ruta **destino** del código (refactor modular), aunque el código de producción
  todavía no se haya movido: `test/SIDAPA.<capa>.test/<TipoDeArtefacto>/<Module>/<Submodule>/<Clase>Tests.cs`
  (p. ej. `test/SIDAPA.application.test/Services/CaseFile/Documents/DocumentServiceTests.cs`).
  La tabla de módulos y submódulos está en `references/layers.md` §1; si `docs/refactor-arquitectura-modular.md`
  existe, manda sobre la tabla. Si una clase no encaja en ningún submódulo, pregunta.
- **Namespace = carpeta**: `SIDAPA.application.test.Services.CaseFile.Documents`.
- **Nombre**: `Metodo_Escenario_ResultadoEsperado`, escenario y resultado en español, como el dominio:
  `UploadAsync_ArchivoDeMasDe20Mb_LoRechazaSinSubirNada`.
- **Arrange / Act / Assert** separados por una línea en blanco. Un comportamiento por prueba
  (varias aserciones están bien si describen el mismo resultado).
- **Shouldly** para aserciones (`x.ShouldBe(…)`, `lista.ShouldHaveSingleItem()`,
  `await Should.ThrowAsync<FileTooLargeException>(…)`). `Assert.*` solo para `Assert.Skip`/`Assert.Fail`.
- Los `using` de `Xunit`, `Shouldly` y `NSubstitute` ya son globales (`test/Directory.Build.props`).
- **CancellationToken**: pasa `TestContext.Current.CancellationToken` a todo método async.
- **Determinismo**: nada de `DateTime.Now`, `Thread.Sleep`, `Task.Delay` para esperar, ni orden de
  ejecución entre pruebas. El tiempo entra por `TimeProvider` (`FixedTimeProvider` en application).
- **Datos**: usa constantes con nombre (`const int ReferenciaId = 7;`) y builders pequeños
  (`Factura()`, `Notice(...)`), no números mágicos repetidos.
- **`[Theory]` + `[InlineData]`** para particiones y límites de la misma regla; un `[Fact]` por escenario distinto.
- Comentario de una línea cuando el **por qué** del caso no es obvio (`// límite exacto: 20 MB sí pasa`).
- Un `/// <summary>` en la clase de prueba que diga qué clase prueba y de qué historia (SCRUM-xx).

**Fakes o NSubstitute.** Usa un **fake** (clase en memoria, en `Fakes/` si lo comparten varias
pruebas) cuando la dependencia guarda estado que el servicio vuelve a leer (repositorios, bucket).
Usa **NSubstitute** para respuestas puntuales (`catalogs.IsActiveAduanaAsync("470").Returns(true)`) y
para verificar interacciones que son el resultado (`await notifications.Received(1).NotifyClientAsync(...)`).
No verifiques con `Received` llamadas que solo son detalle de implementación.

### 6. Corre las pruebas
El repo usa Microsoft.Testing.Platform (`global.json`), así que `dotnet test` lleva `--solution`/`--project`:

```bash
dotnet build SIDAPA.sln
dotnet test --solution SIDAPA.sln                                       # todo
dotnet test --project test/SIDAPA.application.test                      # una capa
dotnet test --project test/SIDAPA.application.test --filter-class "*DocumentServiceTests"
dotnet test --project test/SIDAPA.application.test --filter-method "*Upload*"
```

- Las pruebas de repositorio necesitan **Docker encendido**. Sin Docker se **omiten** (no fallan) y lo
  tienes que decir en el reporte; con `SIDAPA_REQUIRE_DOCKER=1` fallan (para CI).
- Si reproduces un bug: la prueba debe fallar **antes** del arreglo. Si pasa a la primera, no está
  probando el bug.
- Antes de terminar corre el checker (namespace = carpeta, capa correcta, sin `Thread.Sleep`/`DateTime.Now`, etc.):
  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/sidapa-testing/check-tests.ps1
  ```

### 7. Reporta
Termina con este formato (corto, en español):

```
## Pruebas
- Proyecto(s): test/SIDAPA.<capa>.test — N pruebas nuevas, M modificadas
- Resultado: X correctas, Y omitidas (motivo), Z con error
## Casos cubiertos
- <caso> → <por qué importa>
## Riesgos sin cubrir
- <caso> → <por qué no se cubrió / qué haría falta>
## Refactors
- <archivo>: <cambio> (comportamiento sin cambios: lo fijan <pruebas>)
## Bugs encontrados
- <descripción> (prueba que lo demuestra)
```

## Antipatrones (y por qué)

- **Probar el mock**: configurar un substitute y luego afirmar lo que tú mismo configuraste. No prueba nada del SUT.
- **Copiar la implementación en la aserción** (recalcular el resultado con la misma fórmula): si la fórmula está mal, la prueba también.
- **`EF InMemory` o SQLite para repositorios**: no se comportan como PostgreSQL (esquema `sidapa`,
  secuencias, `ExecuteUpdate`, `timestamp without time zone`, índices únicos). Usa `PostgresFixture`.
- **Pruebas que dependen de otras** o de datos que dejó otra prueba en la base compartida: crea tus datos con `TestData`.
- **`try/catch` en la prueba** para afirmar excepciones: usa `Should.ThrowAsync<T>`.
- **Endpoints o páginas manuales de prueba** (`Dev*Controller`, `/dev/...`): lo que se verificaba a
  mano se convierte en prueba automática en la capa correcta.
- **Silenciar una prueba que falla** (`Skip`, comentar, aflojar la aserción) para terminar: si falla, o
  está mal la prueba o está mal el código; averigua cuál y dilo.
