# CLAUDE.md — SIDAPA

Guidance for AI agents and developers working in this repository. Read this before writing code.

## What this project is

SIDAPA automates the creation and management of **pedimentos aduanales** (Mexican customs
declarations) for a customs agency. Thesis project; team: Irvin Marquez and Luis Lobato (backend),
Armando Cadena (front-end and database).

1. **Receive the client's documents** (aviso de arribo: invoices, B/L, packing list…) under a
   **Referencia** that moves through a status flow until the pedimento is validated.
2. **Read invoices automatically** through `sidapa-reader`, a separate Python/FastAPI service
   (separate repo) that returns a **JSON** with the invoice header and lines. SIDAPA maps and persists it.
3. **Build and validate the pedimento** against the **catalogs of Anexo 22 and its apéndices**,
   which are stored and served by SIDAPA.

Domain terminology is Spanish. Code **identifiers are English**; only customs domain nouns stay
Spanish (`Pedimento`, `Partida`, `Aduana`, `Referencia`, `FraccionArancelaria`, `Identificador`,
`RecintoFiscalizado`…). UI text is Spanish; code comments may be Spanish.

## Tech stack

- **.NET 10** (`net10.0`), C# with `Nullable` and `ImplicitUsings` enabled in every project.
- **ASP.NET Core MVC** with **`.cshtml` Razor views** (hard rule below).
- **EF Core 10** (10.0.12) + **PostgreSQL 16** via `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3.
- **JWT** auth (`Microsoft.AspNetCore.Authentication.JwtBearer`), token carried in a cookie.
- **Bootstrap 5.3.3**, local copy in `SIDAPA.presentation/wwwroot/lib/bootstrap/dist` (no CDN).
- **Docker Compose**: `sidapa.presentation`, `db` (postgres:16-alpine), `reader` (built from `../sidapa-reader`).

## ⚠️ Hard rule: `.cshtml` (MVC), NOT Razor Components / Blazor

Use classic **MVC controllers + `.cshtml` views**. Never add `.razor` files, Blazor, or interactive
server components. `Program.cs` is already MVC (`AddControllersWithViews` + `MapControllerRoute`).

## Architecture — Clean Architecture

Dependencies point **inward only**; the domain depends on nothing.

```
SIDAPA.presentation  ──►  SIDAPA.application  ──►  SIDAPA.domain
        │                        ▲
        └───────────────►  SIDAPA.infraestructure  ──► (application, domain)
   (presentation references infrastructure ONLY in Program.cs, for DI wiring)
```

| Project | Contains |
|---|---|
| `SIDAPA.domain` | `Entities/{Core,CustomDeclaration}/{Catalogs,Operative}/*Entity.cs`, `Enums/Core/`, `Rules/Core/` (e.g. `ReferenciaStatusFlow`), `Exceptions/CoreException/` |
| `SIDAPA.application` | `Interface/` (`ITaxScanClient`) and `Interface/Core/` (`IUserService`, `ITokenService`…), `Services/Core/`, `DTOs/Core/Auth/`, `DTOs/TaxScan/` |
| `SIDAPA.infraestructure` | `Persistence/SidapaDbContext.cs`, `Persistence/Configurations/{Core,CustomDeclaration}/…`, `Migrations/`, `Repositories/`, `ApiClients/` (`TaxScanClient`), `Security/` (`JwtOptions`, `JwtTokenServices`), `DependencyInjection.cs` (`AddInfraestructure`) |
| `SIDAPA.presentation` | `Controllers/Web/<Area>/` (pages), `Controllers/Api/` (JSON), `Views/`, `Filters/` (`DomainExceptionFilter`, `AntiforgeryRedirectFilter`), `Security/` (`AuthCookie`, `TokenRenewalMiddleware`), `wwwroot/`, `Program.cs` |

**Rules**
- **Interfaces live in `application`; implementations in `infraestructure`.**
- The **domain** is pure C#: no NuGet, no EF, no ASP.NET, no data annotations. Invariants throw `DomainException`.
- **EF Core stays in `infraestructure`.** Application reaches persistence only through repository interfaces.
- External HTTP clients (the reader) live in `infraestructure/ApiClients`; application only sees
  the interface and DTOs, never raw JSON.
- `presentation` registers everything through `builder.Services.AddInfraestructure(...)`.

**Errors**
- Business errors are **exceptions that inherit from `DomainException`** (e.g. `InvalidCredentialsException`),
  one class per error, in `domain/Exceptions/CoreException/`. Don't return error enums or result objects.
- Controllers don't use try/catch for them: put `[TypeFilter(typeof(DomainExceptionFilter))]` on the POST
  action and the filter re-renders the same view with the message (`asp-validation-summary`).

**Catalogs as enums**
- Fixed system catalogs are **enums in `domain/Enums/Core/`** (`RolesEnum`, `ReferenciaStatusEnum`). Use
  the enum in code, never magic numbers or strings; when a string is needed use `nameof(RolesEnum.GLOSADOR)`
  or `.ToString()`.

## Naming & code conventions

- **Namespaces:** lowercase layer segment (`SIDAPA.domain`, `SIDAPA.application`, `SIDAPA.infraestructure`,
  `SIDAPA.presentation`), then PascalCase, **mirroring the folder path**.
- **Keep the spelling `infraestructure`**. Do not "correct" it.
- File-scoped namespaces; primary constructors where they read cleanly; one public type per file.
- Suffixes: `…Entity`, `…Configuration`, `…Service`/`I…Service`, `…Repository`/`I…Repository`,
  `…Controller`, `…Dto`, `…Enum`.
- `async`/`await` with `CancellationToken` for all I/O.
- **File names are case-sensitive in Docker (Linux).** A view path in code must match the file's exact casing.

## Persistence (EF Core + PostgreSQL)

- All tables live in schema **`sidapa`** (`HasDefaultSchema("sidapa")`). DB names are Spanish
  snake_case; map them with Fluent API (`ToTable`, `HasColumnName`). Entity/property names are English.
- **One `IEntityTypeConfiguration<T>` per entity** under `Persistence/Configurations/...`, loaded with
  `ApplyConfigurationsFromAssembly`. No attributes on entities.
- `serial` keys → `UseIdentityAlwaysColumn()`; catalog `smallint` keys → `ValueGeneratedNever()`;
  timestamps → `"timestamp without time zone"`; name every PK/FK/unique (`HasName`, `HasConstraintName`, `HasDatabaseName`).
- Static reference data (Anexo 22 catalogs, statuses) is **seeded** with `HasData` in its configuration
  (see `StatusConfiguration`). Keep catalog tables separate from transactional pedimento data.
- Store the official Anexo 22 **clave** as-is and validate pedimento input against the catalog.

## Referencia status

`ReferenciaStatusEnum` (`short`, values 1–15) + `ReferenciaStatusFlow` (allowed transitions) in the domain.
Statuses and the **Definition of Done** are documented in `docs/estatus-y-definicion-de-terminado.md`.

## Authentication & roles

- `JwtTokenServices` creates the token; claims: `sub`, `name`, `email`, `role`, `client_id`.
- The token travels in the cookie **`sidapa_token`** (`AuthCookie`: HttpOnly, Secure, SameSite=Strict).
  `Program.cs` reads it in `OnMessageReceived`; `MapInboundClaims = false`, `RoleClaimType = "role"`.
- The `role` claim is the **`RolesEnum` name** (`CAPTURADOR`, `GLOSADOR`…). Protect endpoints with
  `[Authorize]` / `[Authorize(Roles = nameof(RolesEnum.GLOSADOR))]`; the comparison is exact (case-sensitive).
- Sliding renewal at half-life: `TokenRenewalMiddleware`.
- `Controllers/Api/Dev*Controller.cs` are **testing endpoints only**; do not build features on them and
  restrict or remove them before production.

## Document reader (`sidapa-reader`)

- Contract: `POST /invoices/read` (multipart file) → JSON in **snake_case**.
- C# side: typed `HttpClient` `TaxScanClient : ITaxScanClient` with `JsonNamingPolicy.SnakeCaseLower`,
  DTOs in `application/DTOs/TaxScan`.
- Base URL `TaxScan:BaseUrl`: `http://localhost:8000` locally, `http://reader:8000` inside Docker.

## Front-end (views)

- Page controllers in `Controllers/Web/<Area>/`; views in `Views/<Controller>/<Action>.cshtml`;
  shared layouts in `Views/Shared/` (`_Layout.cshtml` with the role menu; `_AuthLayout.cshtml` without menu for login).
- **Use Bootstrap first** (grid, forms, buttons, alerts, utilities). Write custom CSS only for what
  Bootstrap lacks (animations, special shapes), in `wwwroot/css/<feature>/`; JS in `wwwroot/js/`.
  Link Bootstrap **before** custom CSS; use `asp-append-version="true"` on own assets.
- **Colors and sizes come from `wwwroot/css/base/theme.css`** (`--sidapa-*` variables; provisional palette
  until design defines it). Never write colors directly (`#hex`, `rgb`) in other stylesheets.
- Use relative units (`rem`, `%`, `clamp()`), not `px`, and let Bootstrap's grid handle responsiveness
  (`col-12 col-lg-*`, `d-none d-lg-block`) instead of writing media queries.
- **Prefer server rendering over JavaScript:** forms do a normal POST and the server re-renders the view
  with errors. Add JS only for what the server can't do.
- Business logic never goes in views or controllers: controllers call application services.
- Respect `prefers-reduced-motion` for decorative animations.

## Configuration & secrets

- **Never commit secrets.** `appsettings.json` keeps `ConnectionStrings:DefaultConnection` empty and has no `Jwt:Key`.
- Local secrets go in **user-secrets** (`UserSecretsId` in `SIDAPA.presentation.csproj`):
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project SIDAPA.presentation
  dotnet user-secrets set "Jwt:Key" "<min 32 chars>"                      --project SIDAPA.presentation
  ```
- Docker reads `POSTGRES_*` and `JWT_KEY` from `.env` (gitignored; template in `.env.example`) and mounts the
  user-secrets file read-only.
- Don't commit local test artifacts that may contain tokens (e.g. `cookies.txt` from curl).

## Build & run

```bash
dotnet build SIDAPA.sln
dotnet run --project SIDAPA.presentation --launch-profile https
docker compose up -d --build            # db + reader + web (needs ../sidapa-reader next to this repo)
```

EF Core (infrastructure project, presentation as startup):

```bash
dotnet ef migrations add <Name> --project SIDAPA.infraestructure --startup-project SIDAPA.presentation
dotnet ef database update        --project SIDAPA.infraestructure --startup-project SIDAPA.presentation
```

## Git & Jira workflow

- Tasks live in **Jira**, project key **`SCRUM`** (SIDAPA). Branches `dev` (integration) and `main` (releases).
- One branch per task, **created from `dev`**, named `SCRUM-<n>-short-description`.
- Every commit message and PR title starts with the key: `SCRUM-42 agrega política de permisos por rol`.
- PRs target **`dev`**, never `main`; never commit directly to `dev` or `main`. Merge `dev` into the
  branch and run `dotnet build` before opening the PR.
- Jira automation moves the card by itself: branch created → **En curso**, PR opened → **En revisión**,
  PR merged into `dev` → **Finalizada**. Without the key in the branch/PR title nothing moves.
- A task is done only when it meets the Definition of Done (tested, in the repo, documented).

## Working in this repo

- Respect the layer boundaries; if something tempts you to use EF in the domain or map JSON in a
  controller, it belongs in another layer.
- Match the existing style instead of reformatting.
- Ask before adding NuGet dependencies or a new top-level project.

## Glossary

- **Pedimento:** official Mexican customs declaration for import/export.
- **Referencia:** the agency's file for one operation; groups documents, invoices and the pedimento.
- **Partida:** a line of the pedimento (goods grouped by fracción and origin).
- **Aduana:** customs office. **Glosa:** quality review of the pedimento before validation.
- **Anexo 22:** annex of the *Reglas Generales de Comercio Exterior* with the instructions and catalogs
  for filling out a pedimento. **Apéndice:** one of its catalogs. **Clave:** a code from a catalog.
- **Fracción arancelaria / NICO:** tariff classification code and its statistical sub-code.
