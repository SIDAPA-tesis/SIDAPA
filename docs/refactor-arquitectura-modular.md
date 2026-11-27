# Refactor: arquitectura modular (Clean Architecture × módulos)

> **Documento de trabajo vivo.** Lo usa cualquier persona o agente que continúe este refactor en otra sesión.
> Actualiza la sección [Estado](#estado) y la [Bitácora](#bitácora) cada vez que termines un paso.

- **Creado:** 2026-11-23 (revisión de organización hecha con Claude Code).
- **Commit base de la revisión:** `f14becb` (rama `SCRUM-46-aviso-arribo`, con trabajo de aviso de arribo sin commit).
- **Alcance:** solo organización (carpetas, namespaces, límites de capa y de módulo, registro DI). **No** cambia
  comportamiento ni esquema de base de datos.

---

## Cómo usar este documento (léelo primero si eres un agente)

1. Lee `CLAUDE.md` (convenciones del repo) y este archivo completo.
2. Revisa [Estado](#estado) y busca la primera fase con casillas `[ ]` pendientes. **Haz una fase a la vez.**
3. Comprueba los [Prerrequisitos](#prerrequisitos) de esa fase (rama, clave Jira, ramas abiertas).
4. Ejecuta los pasos en orden siguiendo el [Procedimiento técnico](#procedimiento-técnico). Después de cada
   bloque: `dotnet build SIDAPA.sln`.
5. Antes de abrir el PR, pasa la [Verificación](#verificación).
6. Marca las casillas, actualiza la tabla de Estado y añade una línea a la [Bitácora](#bitácora). Incluye
   esta actualización en el mismo PR.
7. Si el código ya no coincide con el mapa (alguien agregó o borró archivos), **aplica la regla de ubicación**
   (sección [Reglas objetivo](#reglas-objetivo)) al archivo nuevo y anótalo en la Bitácora. No inventes módulos nuevos:
   si un archivo no encaja, pregunta al usuario.

---

## Estado

| Fase | Descripción | Rama | Estado | PR |
|---|---|---|---|---|
| 0 | Fugas de capa y registro DI | `SCRUM-??-…` | 🟡 Parcial (3 de 7 hechos en SCRUM-46) | — |
| 1 | Domain + Persistence por módulo | `SCRUM-??-…` | ⬜ Pendiente | — |
| 2 | Application + adaptadores externos | `SCRUM-??-…` | ⬜ Pendiente | — |
| 3 | Presentation (controllers, models, views) | `SCRUM-??-…` | ⬜ Pendiente | — |
| 4 | Documentación y tests | `SCRUM-??-…` | ⬜ Pendiente | — |

Leyenda: ⬜ pendiente · 🟡 en curso/parcial · ✅ terminada (PR mergeado a `dev`).

---

## Decisiones tomadas

| # | Decisión | Motivo |
|---|---|---|
| D1 | Tres módulos: **Core**, **CaseFile**, **CustomDeclaration**. | Core = lo que no depende de ningún módulo (auth, usuarios, datos personales, permisos, avisos, almacenamiento). CaseFile = Referencia/expediente/documentos/estatus/aviso de arribo. CustomDeclaration = pedimento, partidas, facturas, catálogos Anexo 22, claves, productos. |
| D2 | Nombres de carpeta de módulo/submódulo **en inglés**. | Elegido por el usuario. Los nombres de clase con sustantivos aduanales (`ReferenciaEntity`, `PedimentoService`…) **no se renombran**. |
| D3 | Orden de carpetas **tipo primero**: `<Tipo>/<Module>/<Submodule>/`. | Es el patrón que ya usa el repo (`Entities/Core/…`, `Interface/Core/…`) y el que pidió el usuario (`Controllers/Api/{module}/{submodule}`). |
| D4 | Entidades y configuraciones EF usan `Catalogs`/`Operative` como submódulo. Enums, Rules y Exceptions solo llevan módulo (sin submódulo). | Refleja las tablas `cat_`/`ope_`. Hay pocos enums, reglas y excepciones por módulo. |
| D5 | Los controllers `Dev*` van juntos en `Controllers/Dev/{Api,Web}/`, fuera de los módulos. | Así se pueden quitar de una sola vez antes de producción. Sus vistas se quedan en `Views/Dev*/` (ubicación por defecto). |
| D6 | `SidapaDbContextFactory` **se queda en presentation**: excepción documentada, igual que `Program.cs`. | Usa `AddUserSecrets<Program>()`. Moverlo a infra obligaría a duplicar el `UserSecretsId`. Es tooling de diseño y forma parte de la raíz de composición. *(Corrige la propuesta inicial de la revisión.)* |
| D7 | `ReportesController` va en `Core/Users`. | Hoy solo muestra altas de usuarios. Si después reporta pedimentos, se mueve. |
| D8 | El nombre `CaseFile` es provisional. | Se puede renombrar **antes** de empezar la Fase 1. Después ya sale caro. |

---

## Reglas objetivo

### Ubicación
```
<Proyecto>/<TipoDeArtefacto>/<Module>/<Submodule>/Archivo.cs
namespace  = SIDAPA.<capa>.<TipoDeArtefacto>.<Module>.<Submodule>   (siempre igual a la ruta de carpetas)
Module    ∈ { Core, CaseFile, CustomDeclaration }
```

### Dependencias entre módulos (dentro de cada capa)
- **Core no depende de ningún módulo.** Ningún archivo de `…/Core/…` puede usar tipos de `…/CaseFile/…` ni de
  `…/CustomDeclaration/…`, y eso incluye las propiedades de navegación de EF.
- `CaseFile` → `Core`, y los **catálogos** de `CustomDeclaration` (Aduana, OperationType, PedimentoKey…).
- `CustomDeclaration` → `Core`, `CaseFile`.
- Excepción documentada: `ReferenciaEntity` (raíz del agregado) conserva sus navegaciones a `Invoices` y `Pedimento`.

### Capas (sin cambios respecto a `CLAUDE.md`)
- Domain: C# puro, sin NuGet.
- Application: interfaces (puertos), servicios, DTOs. Registra **sus** servicios en `AddApplication()`.
- Infraestructure: implementaciones (EF, repositorios, clientes externos). Registra **sus** implementaciones en `AddInfraestructure()`.
- Presentation: solo toca infraestructura en `Program.cs` y `SidapaDbContextFactory.cs` (D6).

### Submódulos vigentes

| Módulo | Submódulos |
|---|---|
| Core | `Auth`, `Users`, `Permissions`, `Notifications`, `Storage`, `Home`, `Persistence` |
| CaseFile | `ArrivalNotice`, `Referencias`, `Documents`, `Expediente` |
| CustomDeclaration | `Catalogs`, `Pedimentos`, `Claves`, `Invoices`, `Products` |

Si un archivo nuevo no cabe en ninguno, pregunta antes de crear un submódulo.

---

## Hallazgos de la revisión (y su estado)

| # | Hallazgo | Estado |
|---|---|---|
| H1 | El dominio tenía `PackageReference Microsoft.Extensions.DependencyInjection`. | ✅ Resuelto en `f14becb` |
| H2 | `AddApplication()` no se llamaba. | ✅ Resuelto en `f14becb` (`Program.cs:49`) |
| H3 | `IUserActual` no estaba registrado. | ✅ Resuelto en `f14becb` (`Program.cs:53-54`) |
| H4 | `IClaveRepositorio`/`IPedimentoRepositorio` no tenían implementación. | ✅ Resuelto en `a011619` |
| H5 | Infraestructura registra **servicios de aplicación** (`UserService`, `NotificationService`, `DocumentService`, `ExpedienteService`, `DocumentFileValidator`). | ⬜ Fase 0 |
| H6 | ViewComponents de presentación inyectan `SidapaDbContext` (`WidgetNovedadesViewComponent`, `WidgetAccesosRapidosViewComponet`). | ✅ Resuelto (2026-11-25): se borraron y se reemplazaron por `AccesosRapidosViewComponent` (`IPermisosService`) y `NovedadesViewComponent` (`INotificationService`) |
| H7 | Typo `ViewComponets`/`Componets`, y clases con sufijo `…ViewComponet`. MVC no encuentra `Views/Shared/Components/{Name}/Default.cshtml`. | 🟡 Parcial (2026-11-25): `MenuLateralViewComponent` renombrado y su vista ya está en `Views/Shared/Components/MenuLateral/`; falta renombrar la carpeta `ViewComponets/` |
| H8 | Core depende de módulos: `ClientEntity.Referencias`, `ClientEntity.Products`, `NotificationEntity.Referencia`, `SupplierEntity.Country`. | ⬜ Fase 1 |
| H9 | Entidades, enums, reglas y excepciones de negocio están bajo `Core`. | ⬜ Fase 1 |
| H10 | `PersonEntity` tiene el namespace `SIDAPA.domain.Entities`, que no coincide con su carpeta. | ⬜ Fase 1 |
| H11 | Application está todo en `Core` y sin submódulo; `ITaxScanClient` está en la raíz de `Interface/`. | ⬜ Fase 2 |
| H12 | Repositorios planos; adaptadores externos repartidos en `ApiClients/`, `Email/` y `External/`. | ⬜ Fases 1–2 |
| H13 | Nombres: `…Repositorio`, `InvoceLineDto`, `PresentacionExtenciones`; `DTOs/Core/Clave` tiene el namespace `…Claves`. | ⬜ Fases 2–3 |
| H14 | Controllers agrupados por pantalla (`Web/Dashboard`, `Web/SIDAPA`), no por módulo. | ⬜ Fase 3 |
| H15 | El proyecto de tests `SIDAPA.Infraestructure.test` prueba dominio y aplicación, y sus carpetas son planas. | ⬜ Fase 4 |

---

## Prerrequisitos

- [ ] **Terminar y mergear `SCRUM-46-aviso-arribo` a `dev`.** Tiene trabajo sin commit (aviso de arribo:
      `ArrivalNotice*`, `ICatalogRepository`, nuevas excepciones y reglas) que este refactor va a mover.
- [ ] Pedir **una clave Jira por fase** (o subtareas de una épica) y anotarlas en la tabla de Estado.
- [ ] Avisar al equipo (Irvin, Armando). Ramas abiertas que van a chocar con las fases 1–3:
      `SCRUM-49-CONSECUTIVE-FOLIO`, `SCRUM-35-google-cloud-storage`, `refactor/backend-skill` (worktree).
      Lo ideal es mergearlas antes de la Fase 1 o congelar los cambios de backend mientras dura cada fase.
- [ ] Cada fase: rama nueva desde `dev` actualizado (`SCRUM-<n>-refactor-modulos-fase-<k>`), PR a `dev`.

---

## Fase 0 — Fugas de capa y registro DI (sin mover módulos)

- [x] Quitar el NuGet del dominio (H1).
- [x] Llamar `AddApplication()` en `Program.cs` (H2).
- [x] Registrar `IUserActual → UsuarioActual` + `AddHttpContextAccessor` (H3).
- [ ] **H5.** Mover estos registros de `SIDAPA.infraestructure/DependencyInjection.cs` a
      `SIDAPA.application/DependencyInjection.cs` (`AddApplication`):
      `IUserService→UserService`, `IDocumentFileValidator→DocumentFileValidator` (singleton),
      `INotificationService→NotificationService`, `IDocumentService→DocumentService`,
      `IExpedienteService→ExpedienteService` y, si ya existe, `IArrivalNoticeService→ArrivalNoticeService`.
      En infraestructura se quedan solo repositorios, clientes externos, seguridad y email.
      Conserva el mismo *lifetime* de cada registro.
- [x] **H6.** En `WidgetNovedadesViewComponent` y `WidgetAccesosRapidosViewComponet`: quitar la inyección de
      `SidapaDbContext` y los `using` de EF e infraestructura. Las consultas ya están comentadas: deja un
      `// TODO: usar I<Servicio> de application` y que devuelvan la vista vacía o con datos de ejemplo.
- [ ] **H7.** `git mv SIDAPA.presentation/ViewComponets SIDAPA.presentation/ViewComponents`. Renombrar
      `MenuLateralViewComponet → MenuLateralViewComponent` y `WidgetAccesosRapidosViewComponet → WidgetAccesosRapidosViewComponent`
      (archivo y clase). Namespace: `SIDAPA.presentation.ViewComponents`.
      Mover `Views/Shared/Componets/MenuLateral/Default.cshtml` → `Views/Shared/Components/MenuLateral/Default.cshtml`
      (la carpeta `Components/` ya existe vacía) y borrar `Componets/`.
- [ ] Verificar que `grep -rln "SIDAPA.infraestructure\|EntityFrameworkCore" SIDAPA.presentation --include=*.cs`
      solo devuelve `Program.cs` y `SidapaDbContextFactory.cs`.

---

## Fase 1 — Domain + Persistence por módulo

### 1.1 Entidades (`SIDAPA.domain/Entities/…`)
| Origen | Destino |
|---|---|
| `Core/Catalogs/DocumentTypeEntity`, `RequiredDocumentEntity`, `StatusEntity` | `CaseFile/Catalogs/` |
| `Core/Catalogs/OperationTypeEntity` | `CustomDeclaration/Catalogs/` |
| `Core/Operative/ReferenciaEntity`, `DocumentEntity`, `DocumentRequestEntity`, `StatusHistoryEntity` | `CaseFile/Operative/` |
| `Core/Operative/SupplierEntity` | `CustomDeclaration/Operative/` |
| Se quedan en Core: `RoleEntity`, `SystemParameterEntity`, `ClientEntity`, `NotificationEntity`, `UserEntity`, `PersonEntity` | — (corregir namespace de `PersonEntity` → `SIDAPA.domain.Entities.Core.Operative`) |

### 1.2 Enums (`SIDAPA.domain/Enums/…`)
- `Core/`: `RolesEnum`, `ModuleEnum`
- `CaseFile/`: `DocumentStatusEnum`, `DocumentTypeEnum`, `ReferenciaStatusEnum`
- `CustomDeclaration/`: `ClaveStateEnum`, `ClaveTypeEnum`, `OperationTypeEnum`, `UrgenciaEnum`

### 1.3 Reglas (`SIDAPA.domain/Rules/…`)
- `Core/`: `LoginLockoutRule`, `NotificationRule`
- `CaseFile/`: `ArrivalNoticeRule`, `DocumentFileRule`, `DocumentPathRule`, `ExpedienteRule`, `ReferenciaNumberRule`, `ReferenciaStatusFlow`
- `CustomDeclaration/`: `PedimentoUrgencyRule`

### 1.4 Excepciones (`SIDAPA.domain/Exceptions/CoreException/` → `Exceptions/<Module>/`)
- `Core/`: `DomainException`, `AccountLockedException`, `InvalidCredentialsException`, `UserNotFoundException`,
  `EmptyNotificationMessageException`, `NotificationMessageTooLongException`, `NotificationNotFoundException`
- `CaseFile/`: `ArrivalDateRequiredException`, `DocumentNotFoundException`, `EmptyFileException`,
  `FileContentMismatchException`, `FileTooLargeException`, `FileTypeAllowedException`, `InvalidDocumentTypeException`,
  `NoDocumentsException`, `NotesTooLongException`, `OperationTypeNotAllowedException`, `ReferenciaNotFoundException`
- `CustomDeclaration/`: `AduanaNotFoundException`
- Borrar la carpeta `CoreException/`. `DomainExceptionFilter` pasa a usar `SIDAPA.domain.Exceptions.Core`.

### 1.5 Romper dependencias Core → módulo (H8). Sin cambios de esquema.
- [ ] Borrar `ClientEntity.Referencias`. En `ReferenciaConfiguration` (≈l.31), `.WithMany(x => x.Referencias)` → `.WithMany()`.
- [ ] Borrar `ClientEntity.Products`. En `ProductConfiguration` (≈l.26), `.WithMany(x => x.Products)` → `.WithMany()`.
- [ ] Borrar `NotificationEntity.Referencia` (se queda `ReferenciaId`). Quitar el `HasOne(x => x.Referencia)…` de
      `NotificationConfiguration` (≈l.28-30) y declararlo desde `ReferenciaConfiguration`:
      `builder.HasMany(x => x.Notifications).WithOne().HasForeignKey(n => n.ReferenciaId)` con **el mismo**
      `HasConstraintName` y `OnDelete` que tenía.
- [ ] `SupplierEntity.Country` queda resuelto al mover `SupplierEntity` a CustomDeclaration (1.1).
- [ ] Buscar usos de esas navegaciones (`grep -rn "\.Referencias\b\|\.Products\b\|n\.Referencia\b"`) en repositorios
      y servicios, y reescribirlos con joins o `Where` por FK.

### 1.6 Persistencia (`SIDAPA.infraestructure/`)
- `Persistence/Configurations/` refleja 1.1: `Core/Catalogs/{DocumentType,RequiredDocument,Status}Configuration` →
  `CaseFile/Catalogs/`; `Core/Catalogs/OperationTypeConfiguration` → `CustomDeclaration/Catalogs/`;
  `Core/Operative/{Document,DocumentRequest,Referencia,StatusHistory}Configuration` → `CaseFile/Operative/`;
  `Core/Operative/SupplierConfiguration` → `CustomDeclaration/Operative/`.
- `Repositories/` → `Repositories/<Module>/<Submodule>/`:
  `UserRepository` → `Core/Users`, `NotificationRepository` → `Core/Notifications`,
  `DocumentRepository` → `CaseFile/Documents`, `ReferenciaRepository` → `CaseFile/Referencias`,
  `CatalogRepository` → `CustomDeclaration/Catalogs`, `ClaveRepository` → `CustomDeclaration/Claves`,
  `PedimentoRepository` → `CustomDeclaration/Pedimentos`.
- `SidapaDbContext`, `DatabaseMigrator` y `Seed/` se quedan donde están.

### 1.7 Migración de control
- [ ] `dotnet ef migrations add ReorganizeModules --project SIDAPA.infraestructure --startup-project SIDAPA.presentation`
- [ ] **`Up()` y `Down()` deben quedar vacíos.** Si hay cualquier operación (drop/add FK, rename…), revierte la migración
      (`dotnet ef migrations remove …`) y corrige el mapeo hasta que salga vacía. Luego conserva la migración vacía:
      solo actualiza el snapshot.

---

## Fase 2 — Application + adaptadores externos

### 2.1 Application (`Interface/`, `Services/`, `DTOs/` → `<Tipo>/<Module>/<Submodule>/`)
| Submódulo | Interface | Services | DTOs |
|---|---|---|---|
| `Core/Auth` | `ITokenService`, `IPasswordHasher`, `IUserActual` | — | `AuthToken`, `UserSessionDto` |
| `Core/Users` | `IUserRepository`, `IUserService` | `UserService` | — |
| `Core/Permissions` | `IPermisosService` | `PermisosService` | — |
| `Core/Notifications` | `INotificationRepository`, `INotificationService`, `IEmailSender` | `NotificationService` | `EmailMessageDto`, `NotificationDto` |
| `Core/Storage` | `ICloudStorageService` (desde `Interface/External/CloudStorage`) | — | `CloudStorage*` (desde `DTOs/External/CloudStorage`) |
| `CaseFile/ArrivalNotice` | `IArrivalNoticeService` | `ArrivalNoticeService` (si existe) | `ArrivalNoticeDtos` (sin `AduanaOptionDto`) |
| `CaseFile/Referencias` | `IReferenciaRepository` | — | — |
| `CaseFile/Documents` | `IDocumentRepository`, `IDocumentService`, `IDocumentFileValidator` | `DocumentService`, `DocumentFileValidator` | `DocumentDto`, `UploadDocumentDto` |
| `CaseFile/Expediente` | `IExpedienteService` | `ExpedienteService` | `ExpedienteStatusDto` |
| `CustomDeclaration/Catalogs` | `ICatalogRepository` | — | `AduanaOptionDto` (sacarlo de `ArrivalNoticeDtos.cs` a su propio archivo) |
| `CustomDeclaration/Claves` | `IClaveRepository` (renombrar desde `IClaveRepositorio`), `IClaveService` | `ClaveService` | `ClaveDto` (desde `DTOs/Core/Clave`) |
| `CustomDeclaration/Pedimentos` | `IPedimentoRepository` (renombrar desde `IPedimentoRepositorio`), `IPedimentoService` | `PedimentoService` | `PedimentoDto`, `PedimentoResumenDto` |
| `CustomDeclaration/Invoices` | `ITaxScanClient` (desde la raíz `Interface/`) | — | `InvoiceLineDto` (renombrar desde `InvoceLineDto`), `InvoiceReadResultDto`, `SupplierDto` (desde `DTOs/TaxScan`) |

Borrar las carpetas vacías `Interface/External`, `DTOs/External` y `DTOs/TaxScan`.

### 2.2 Adaptadores en infraestructura
- `ApiClients/TaxScanClient.cs` → `External/TaxScan/TaxScanClient.cs`
- `Email/{EmailOptions,LogEmailSender,SmtpEmailSender}.cs` → `External/Email/`
- `External/CloudStorage/` se queda. `Security/` se queda (es la implementación técnica de Core/Auth).
- Actualizar los `using` de `Program.cs` y `DependencyInjection.cs`.

---

## Fase 3 — Presentation

### 3.1 Controllers
| Origen | Destino |
|---|---|
| `Web/Auth/AuthController` | `Web/Core/Auth/` |
| `Web/SIDAPA/IndexController`, `Web/Dashboard/HomeController` | `Web/Core/Home/` |
| `Web/Dashboard/UsuariosController`, `ReportesController` | `Web/Core/Users/` |
| `Web/Dashboard/PedimentosController` | `Web/CustomDeclaration/Pedimentos/` |
| `Web/Dashboard/ClavesController` | `Web/CustomDeclaration/Claves/` |
| `Web/Dashboard/ProductosController` | `Web/CustomDeclaration/Products/` |
| Controller de aviso de arribo (SCRUM-46) | `Web/CaseFile/ArrivalNotice/` |
| `Web/Dev/*` | `Dev/Web/` |
| `Api/Dev*` | `Dev/Api/` |

Los controllers de API reales van en `Controllers/Api/<Module>/<Submodule>/` con la ruta `api/<module>/<submodule>`
(en minúsculas). Hoy no hay ninguno. Los nombres de clase **no cambian**, así que las rutas convencionales MVC tampoco.

### 3.2 Models
`Auth/LoginViewModel` → `Core/Auth/`; `InicioViewModel` → `Core/Home/`; `Dashboard/{Usuarios,ReporteMensual}` → `Core/Users/`;
`Claves/*` → `CustomDeclaration/Claves/`; `Pedimentos/*` → `CustomDeclaration/Pedimentos/`; `Dev/*` → `Dev/`;
`MenuLateralViewModel` y `PresentacionExtenciones` (renombrar a `PresentationExtensions`) → `Shared/`.

### 3.3 Vistas: `Views/<Module>/<Controller>/<Action>.cshtml`
Crear `SIDAPA.presentation/Mvc/ModuleViewLocationExpander.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace SIDAPA.presentation.Mvc;

/// <summary>Busca vistas en Views/{Module}/{Controller}/ según el namespace Controllers.Web.{Module}.*</summary>
public sealed class ModuleViewLocationExpander : IViewLocationExpander
{
    private const string Key = "module";
    private const string Marker = ".Controllers.Web.";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        if (context.ActionContext.ActionDescriptor is not ControllerActionDescriptor action) return;
        var ns = action.ControllerTypeInfo.Namespace ?? string.Empty;
        var i = ns.IndexOf(Marker, StringComparison.Ordinal);
        if (i >= 0) context.Values[Key] = ns[(i + Marker.Length)..].Split('.')[0];
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations) =>
        context.Values.TryGetValue(Key, out var module) && !string.IsNullOrEmpty(module)
            ? viewLocations.Prepend($"/Views/{module}/{{1}}/{{0}}.cshtml")
            : viewLocations;
}
```
Registro en `Program.cs`:
`builder.Services.Configure<RazorViewEngineOptions>(o => o.ViewLocationExpanders.Add(new ModuleViewLocationExpander()));`

Movimientos (respeta mayúsculas exactas: Docker corre en Linux):
| Origen | Destino |
|---|---|
| `Views/Auth/*` | `Views/Core/Auth/` |
| `Views/SIDAPA/Index.cshtml` | `Views/Core/Index/Index.cshtml`, y quitar la ruta fija `"~/Views/SIDAPA/Index.cshtml"` en `IndexController` (≈l.25) |
| `Views/Home/*` | `Views/Core/Home/` |
| `Views/Usuarios/*`, `Views/Reportes/*` | `Views/Core/Usuarios/`, `Views/Core/Reportes/` |
| `Views/Pedimentos/*` (incluye `_TarjetaPedimento`, `_ViewImports`) | `Views/CustomDeclaration/Pedimentos/` |
| `Views/Claves/*` (incluye `_ViewImports`) | `Views/CustomDeclaration/Claves/` |
| `Views/Productos/*` | `Views/CustomDeclaration/Productos/` |
| `Views/Dev*/`, `Views/Shared/`, `_ViewImports`, `_ViewStart` | se quedan |

Actualizar los `@using`/`@model` de las vistas y de los `_ViewImports` según los nuevos namespaces de Models y DTOs.

---

## Fase 4 — Documentación y tests

- [ ] `CLAUDE.md`: actualizar la tabla de capas con las rutas `<Tipo>/<Module>/<Submodule>`, añadir las reglas de
      dependencia entre módulos, `Controllers/Dev/`, `AddApplication` vs `AddInfraestructure`, la excepción D6 y el
      `ModuleViewLocationExpander`. Quitar el puntero "Refactor en curso".
- [ ] Tests: reorganizar en carpetas `<Module>/<Submodule>`. Valorar separar `SIDAPA.domain.test` y
      `SIDAPA.application.test` del proyecto actual (pregunta antes: agrega proyectos).
- [ ] Marcar este documento como ✅ terminado (o archivarlo).

---

## Procedimiento técnico

- **Mover siempre con `git mv`** para conservar el historial. Nunca borrar y recrear.
- Después de mover, ajustar el `namespace` a la ruta (Rider: *Refactor → Adjust Namespaces* sobre la carpeta) y
  arreglar los `using` hasta que `dotnet build SIDAPA.sln` compile.
- Un commit por bloque (p. ej. "entidades CaseFile", "excepciones"), con el mensaje `SCRUM-<n> …`.
- No cambiar lógica, firmas públicas (salvo los renombres listados) ni nombres de tabla o columna.
- Script de control: namespace = carpeta (ejecútalo en Git Bash desde la raíz):
  ```bash
  grep -rn "^namespace" --include=*.cs SIDAPA.domain SIDAPA.application SIDAPA.infraestructure SIDAPA.presentation \
    | grep -v -E "/obj/|Migrations/" | sed -E 's/:[0-9]+:namespace / -> /; s/;$//' \
    | awk '{f=$1; ns=$3; d=f; sub(/\/[^\/]*$/,"",d); gsub(/\//,".",d); if (tolower(d)!=tolower(ns)) print "MISMATCH " f " -> " ns}'
  ```
  Debe salir vacío. Archivos de nivel raíz como `Program.cs` o `DependencyInjection.cs` usan el namespace del proyecto.
- Control "Core no depende de módulos":
  ```bash
  grep -rnE "\.(CaseFile|CustomDeclaration)\b" --include=*.cs $(find SIDAPA.* -type d -path "*/Core" -not -path "*/obj/*")
  ```
  Debe salir vacío.

---

## Verificación (al cerrar cada fase)

1. `dotnet build SIDAPA.sln` sin errores ni advertencias nuevas.
2. `dotnet test test/SIDAPA.Infraestructure.test`.
3. Los dos scripts de control de arriba salen vacíos para lo ya migrado.
4. Fase 1: la migración `ReorganizeModules` tiene `Up`/`Down` vacíos.
5. `dotnet run --project SIDAPA.presentation --launch-profile https`: login, landing, `/Home`, `/Pedimentos`,
   `/Claves`, `/Usuarios`, `/Reportes`, `/Productos`, menú lateral y páginas `dev/*` renderizan. Esto prueba DI y la
   ubicación de vistas.
6. `docker compose up -d --build` y repetir el punto 5 (valida mayúsculas en Linux y migraciones al arrancar).
7. Fases 0 y 3: `.claude/skills/sidapa-frontend/check-frontend.ps1 -Include '<vistas tocadas>'`.

---

## Fuera de alcance

- Typos en `wwwroot/css/componets/` y otros assets del front: los decide front-end (Armando).
- Lógica de negocio, seguridad, rendimiento y esquema de base de datos.
- Quitar los controllers `Dev*`: se juntan en la Fase 3; quitarlos es otra tarea.

---

## Bitácora

| Fecha | Quién | Qué |
|---|---|---|
| 2026-11-23 | Luis Lobato + Claude Code | Revisión de organización y creación de este plan. H1–H4 ya resueltos en `SCRUM-46` (`f14becb`, `a011619`). |
| 2026-11-23 | Luis Lobato + Claude Code | SCRUM-46 (`290d316`): `IArrivalNoticeService` ya se registra en `AddApplication` (parte de H5). Archivos nuevos de presentación con la regla objetivo: `Controllers/Web/CaseFile/ArrivalNotice/AvisosController`, `Models/CaseFile/ArrivalNotice/`. Las vistas quedan en `Views/Avisos/` hasta que exista `ModuleViewLocationExpander` (Fase 3 → `Views/CaseFile/Avisos/`). Nuevo DTO `DocumentTypeOptionDto` en `ArrivalNoticeDtos.cs` (Fase 2: va con `AduanaOptionDto` a `CustomDeclaration/Catalogs` o a `CaseFile/Documents`). |
| 2026-11-24 | Luis Lobato + Claude Code | SCRUM-64 (HU-05): nuevo submódulo **`Core/Persistence`** (aprobado) para `IUnitOfWork`/`IUnitOfWorkTransaction` (`application/Interface/Core/Persistence/`); la implementación `UnitOfWork` queda en `infraestructure/Persistence/`. Archivos nuevos ya en ruta destino: `domain/Rules/CaseFile/DocumentRequestRule`, `domain/Exceptions/CaseFile/` (3 excepciones), `application/{Interface,Services,DTOs}/CaseFile/Documents/` (`IDocumentRequestRepository`, `IDocumentRequestService`, `DocumentRequestService`, DTOs), `infraestructure/Repositories/CaseFile/Documents/DocumentRequestRepository`, `presentation/Controllers/Web/CaseFile/Documents/SolicitudesController`, `presentation/Models/CaseFile/Documents/`. `IDocumentRequestService` se registra en `AddApplication`. Vistas en `Views/Solicitudes/` (Fase 3 → `Views/CaseFile/Solicitudes/`). ViewComponent nuevo `SolicitudesPendientesViewComponent` con el sufijo bien escrito, en la carpeta `ViewComponets/` (H7). |
| 2026-11-25 | Luis Lobato + Claude Code | Menú lateral y tarjetas por rol con un solo servicio: `_Sidebar.cshtml` usa `MenuLateralViewComponent` (`IPermisosService` + `ModuloCatalogo`); se borraron los 8 partials `_Accesos*`/`_novedades*` por rol. **H6 resuelto**, **H7 parcial** (clase y vista; la carpeta `ViewComponets/` se queda como pide la skill de front-end). Archivos nuevos en ruta destino: `presentation/Models/Core/Permissions/` (`ModuloViewModel`, `ModuloCatalogo`, `MenuLateralViewModel`, `OpcionMenuViewModel`, `AccesosRapidosViewModel`) y `presentation/Models/Core/Notifications/` (`NovedadesViewModel`, `NovedadViewModel`); se borró `Models/MenuLateralViewModel.cs`. Pruebas: `application.test/Services/Core/Permissions/`, `presentation.test/Models/Core/{Permissions,Notifications}/`, `presentation.test/Security/DashboardRoutesTests`. |
| 2026-11-26 | Luis Lobato + Claude Code | SCRUM-55 (HU-03, revisión del expediente). Archivos nuevos en ruta destino: `domain/Rules/CaseFile/DocumentReviewRule`, `domain/Exceptions/CaseFile/` (2 excepciones), `domain/Enums/CaseFile/DocumentPreviewKindEnum` (primer enum de CaseFile), `application/{Interface,Services,DTOs}/CaseFile/Expediente/` (`IExpedienteRepository`, `IExpedienteReviewService`, `ExpedienteReviewService`, DTOs), `application/Interface/CaseFile/Documents/ISpreadsheetPreviewReader` + `DTOs/CaseFile/Documents/{Spreadsheet,Sheet}PreviewDto`, `infraestructure/Repositories/CaseFile/Expediente/ExpedienteRepository`, `infraestructure/External/Spreadsheets/ExcelSpreadsheetPreviewReader` (NuGet `ExcelDataReader` 3.7.0, aprobado), `presentation/Controllers/Web/CaseFile/Expediente/ExpedientesController`, `presentation/Models/CaseFile/Expediente/`. Vistas en `Views/Expedientes/` (Fase 3 → `Views/CaseFile/Expedientes/`). Se tocaron sin moverlos `ExpedienteRule` y `DocumentFileRule` (`Rules/Core`, Fase 1 → `Rules/CaseFile`). `IExpedienteReviewService` se registra en `AddApplication`; `IExpedienteService` sigue en infraestructura (H5). |
