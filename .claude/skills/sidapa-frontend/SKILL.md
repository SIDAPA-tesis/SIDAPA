---
name: sidapa-frontend
description: Políticas front-end de SIDAPA (ASP.NET Core MVC + .cshtml + Bootstrap). Úsala al crear o editar una vista, página, .cshtml, partial, ViewComponent, tabla, row, card, CSS, JS o maquetado en SIDAPA.presentation, y para revisar o validar el front-end con el checker (CSS de máximo 300 líneas, imágenes con loading lazy, 1 CSS por página, components sin imports).
---

# SIDAPA front-end

Las rutas son relativas a la raíz del repo (`SIDAPA/`). Estas reglas complementan la sección
"Front-end (views)" de `CLAUDE.md`. El **checker** `check-frontend.ps1` (en esta carpeta) revisa
automáticamente las reglas que se pueden medir. Córrelo antes de dar por terminada cualquier
tarea de vistas o CSS.

## Checker (primero esto)

```powershell
# Solo los archivos que tocaste (lo normal en una tarea). Exit 0 = limpio.
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/sidapa-frontend/check-frontend.ps1 -Include 'Views/Usuarios/*','wwwroot/css/vistas/usuarios.css'

# Todo el proyecto, solo errores
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/sidapa-frontend/check-frontend.ps1 -NoWarnings

# Todo el proyecto con warnings; -Strict hace que los warnings también fallen
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/sidapa-frontend/check-frontend.ps1 -Strict
```

Cada hallazgo sale como `NIVEL REGLA ruta:línea mensaje` (la ruta es relativa a `SIDAPA.presentation/`).
Exit 1 si hay errores. `-Root <carpeta>` revisa otra carpeta (así se probó con fixtures).

| Regla | Nivel | Política |
|---|---|---|
| `CSS-LINES` | error | Máximo 300 líneas por hoja `.css` |
| `IMG-LAZY` | error | Toda `<img>` lleva `loading="lazy"` |
| `CSS-PER-PAGE` | error | Una vista de página carga como máximo 1 CSS propio (los de `~/lib/` no cuentan) |
| `COMPONENT-ASSETS` | error | Partials y ViewComponents no tienen `<link>`, `<script>` ni `@section` |
| `NO-RAZOR` | error | No se permiten archivos `.razor` |
| `HARDCODED-COLOR` | warning | No se escriben `#hex`, `rgb()` ni `hsl()` fuera de `css/base/theme.css` |
| `INLINE-JS` | warning | Nada de `onclick=`/`on*=`, ni de `<script>` en línea en las páginas |
| `PX-UNITS` | warning | Se usa `rem`/`%`/`clamp()` en vez de `px` (se permiten `0px` y `1px`) |

El checker no puede medir las reglas 4, 5 y 6. Esas se revisan a mano.

## Reglas

**1. CSS ≤ 300 líneas por archivo.** Si una hoja se acerca al límite, primero cambia CSS propio por
utilidades de Bootstrap. Si aun así no alcanza, divídela por component y súbela al layout. No se
parte en `pagina-1.css` / `pagina-2.css` (eso rompería la regla 3).

**2. Imágenes con carga diferida.**
```cshtml
✅ <img src="@Model.ImagenUrl" alt="Logo de @Model.Empresa" loading="lazy" width="64" height="64" />
❌ <img src="@Model.ImagenUrl" />
```
Lleva `alt` siempre (`alt=""` si es decorativa) y `width`/`height` cuando se conozcan, para que la página no salte al cargar.

**3. Layout + como máximo 1 CSS propio por página.** Lo que hereda del layout (Bootstrap,
`theme.css`, menú) no cuenta. El CSS propio vive en `wwwroot/css/vistas/<vista>.css` y se declara
**una sola vez** en la vista:
```cshtml
@section Styles {
    <link rel="stylesheet" href="~/css/vistas/usuarios.css" asp-append-version="true" />
}
```
❌ Cargar `estados.css` + `tarjeta.css` + `calendario.css` + `pedimentos.css` en la misma página.
Si un estilo lo usan varias páginas, va en el layout. Si es de una sola, va en el CSS de esa página.

**4. Bootstrap primero.** Grid (`row`, `col-12 col-lg-6`), `card`, `table table-hover`, `badge`,
`btn`, `form-control`, utilidades (`d-flex`, `gap-3`, `mb-4`, `text-muted`). CSS propio solo para
lo que Bootstrap no tiene. Colores siempre con `var(--sidapa-*)` de `wwwroot/css/base/theme.css`.
Responsividad con las clases de Bootstrap, no con `@media`. Ejemplos de referencia que ya siguen
estas reglas: `Views/Pedimentos/` (+ `css/vistas/pedimentos.css`), `Views/Avisos/`, `Views/Claves/` y
`Views/Home/` (estas dos sin CSS propio). El "primary" de Bootstrap (`text-bg-primary`, `border-primary`,
`.progress`, enlaces) ya es el primario de la paleta: lo conecta `theme.css`, no lo repitas por pantalla.
`.btn`, `table`, `th`/`td` no se redefinen en ninguna hoja: un selector global así pisa a Bootstrap en todo
el dashboard (antes `slideMenu.css` pintaba de verde todos los `.btn`, incluso `btn-outline-*`).

Estados con color (urgencia, vigencia…): usa los colores contextuales de Bootstrap (`danger`,
`warning`, `success`, `secondary`) con una extensión que traduzca el enum, como
`UrgenciaEnum.ColorBootstrap()` en `Models/PresentacionExtenciones.cs`:
```cshtml
<span class="badge rounded-pill text-bg-@Model.Urgencia.ColorBootstrap()">@Model.Urgencia.Etiqueta()</span>
<li class="bg-@color-subtle text-@color-emphasis">…</li>
```
No inventes clases propias para eso (`insignia--error`, `marca--aviso`).

Paneles laterales y diálogos que abre el servidor (p. ej. `?clave=A1` en Claves): `offcanvas offcanvas-end show`
o `modal` con `show`, y el cierre es un `<a class="btn-close">` a la URL sin el parámetro. Sin JS ni CSS propio.

**5. JS mínimo.** Filtros, búsquedas y paginación se hacen con `<form method="get">`, y las
acciones con `<form method="post">` + `asp-*`. El servidor vuelve a renderizar la vista (así lo
hacen ya los buscadores de `Views/Pedimentos/Index.cshtml` y `Views/Claves/Index.cshtml`). Los
componentes interactivos de Bootstrap (dropdown, modal, collapse) se manejan con `data-bs-*`, sin
escribir JS. Solo lo que el servidor no puede hacer va a `wwwroot/js/<vista>.js`, cargado en
`@section Scripts`.

**6. ViewModel + sintaxis Razor.** Toda vista declara `@model` con un ViewModel en
`SIDAPA.presentation/Models/<Area>/<X>ViewModel.cs`. El controller llama a un servicio de
application y arma el ViewModel, como en `PanelPedimentosViewModel.Desde(dto, busqueda)` de
`Models/Pedimentos/PanelPedimentosViewModel.cs`. En la vista se usan tag helpers (`asp-controller`,
`asp-action`, `asp-route-id`, `asp-for`, `asp-validation-summary`), `@if` / `@foreach`. No se pasan
datos por `ViewBag`/`ViewData` (salvo `Title` y `ClasePagina`, que son para el layout), ni se pone
lógica de negocio en la vista.

**7. Components para lo repetitivo que viene del backend.**
- **Partial view** (opción por defecto): rows de tabla, cards, artículos, secciones repetidas.
  Archivo `Views/<Controller>/_<Nombre>.cshtml` (o `Views/Shared/` si lo usan varios controllers),
  con `@model` tipado al DTO o ViewModel del elemento. Ejemplo existente: `Views/Pedimentos/_TarjetaPedimento.cshtml`.
  ```cshtml
  <table class="table table-hover align-middle">
      <tbody>
          @foreach (var usuario in Model.Usuarios)
          {
              <partial name="_FilaUsuario" model="usuario" />
          }
      </tbody>
  </table>
  ```
- **ViewComponent**: solo cuando el bloque **obtiene sus propios datos** (menú lateral, widget de
  novedades). Va en `SIDAPA.presentation/ViewComponets/<Nombre>ViewComponent.cs`, inyecta un
  **servicio o interfaz de application** (como `MenuLateralViewComponent` con `IPermisosService`),
  **nunca `SidapaDbContext`**, y su vista va en `Views/Shared/Components/<Nombre>/Default.cshtml`.
- **Nunca dentro de un component**: `<link>`, `<script>`, `@section`. Sus estilos van en el CSS
  de la página o del layout, y su JS en el `.js` de la página.
- **Fuera de los components**: lo que no se repite (encabezado de la página, formulario de filtros único).

**8. Comentarios solo para lo no obvio.** Ni `<summary>` ni `@* … *@` que repitan el nombre: nada de
"true si…" sobre un `bool` (`PuedeMarcar`), ni de describir cada propiedad de un ViewModel o cada partial.
Se comenta el porqué (una regla de negocio, un gotcha de Bootstrap, por qué hay JS).

## Receta: página nueva

1. `Models/<Area>/<X>ViewModel.cs`: un `record` con lo que la vista muestra, y un `Desde(...)` si se arma desde un DTO.
2. Una acción en `Controllers/Web/<Area>/<X>Controller.cs` que llama al servicio y regresa `View(viewModel)`.
3. `Views/<X>/Index.cshtml` con `@model`, `ViewData["Title"]`, `Layout` y el maquetado con Bootstrap.
4. Un partial `_<Elemento>.cshtml` por cada cosa que se repite en un `@foreach`.
5. Si hace falta, un solo `wwwroot/css/vistas/<x>.css` en `@section Styles`.
6. Correr el checker con `-Include` sobre los archivos tocados → exit 0. Después correr `dotnet build SIDAPA.sln`.

## Gotchas (verificados en el repo)

- **Los tres layouts renderizan `Styles` y cargan Bootstrap → `theme.css` → su CSS → `Styles`.** La sección
  se llama `Styles` (no `Estilos`): con otro nombre ningún layout la pinta y la página sale sin estilos sin
  dar error. `_dashboardLayout` e `_inicioLayout` también cargan `bootstrap.bundle.min.js`, así que
  `data-bs-*` (dropdown, collapse, modal) funciona sin JS propio.
- **CSS de cada layout.** `_dashboardLayout` → `css/componets/slideMenu.css` (solo el armazón: menú lateral,
  barra superior, `.page-content`, cerrar sesión). `_inicioLayout` → `css/layout/landingPage/inicio-*.css`.
  `_AuthLayout` → nada extra. Lo de una pantalla nunca va en la hoja del layout.
- **Ancho de la página en `_dashboardLayout`.** `.page-content` tiene `max-width: var(--sidapa-page-width)`
  (60rem). Una página con panel lateral o tablas anchas pone `ViewData["ClasePagina"] = "page-content--ancha"`
  (regla en `slideMenu.css`); el layout la agrega al `<main>`. Así lo hacen Inicio, Pedimentos y Claves.
- **Tema oscuro.** La portada tiene "Cambiar tema" (`data-bs-theme`, `js/tema.js`). `theme.css` solo
  sobreescribe textos/fondos/bordes de Bootstrap en tema claro (`:root:not([data-bs-theme="dark"])`); si
  agregas un `--bs-*` de color, ponlo ahí y no en `:root`, o el modo oscuro deja de funcionar.
- **Corte móvil del dashboard = `md` de Bootstrap (767.98px).** Está en tres lugares que deben coincidir:
  el script en línea del `<head>` de `_dashboardLayout`, `js/dashboard.js` y el `@media` de `slideMenu.css`
  (`47.99875rem`).
- **Utilidades de Bootstrap con `!important`.** `border-0`, `shadow-sm`, `shadow`, `bg-*`, `p-*`… llevan
  `!important`, así que un `:hover` en tu CSS no las puede cambiar. Si el elemento anima borde o sombra
  (como `.tarjeta-pedimento`), no pongas esas utilidades en el HTML: decláralas en tu CSS con las
  variables de Bootstrap (`box-shadow: var(--bs-box-shadow-sm)`). Para `text-truncate` dentro de un
  flex usa `overflow-hidden` en el hijo (Bootstrap no tiene `min-w-0`).
- **Convención de nombres de ViewComponents.** MVC quita el sufijo exacto `ViewComponent` y busca la vista en
  `Views/Shared/Components/<Nombre>/Default.cshtml`. Con el sufijo mal escrito (`…ViewComponet`) la
  clase se registra con otro nombre y la vista no se encuentra. La carpeta de clases `ViewComponets/`
  se queda como está.
- **Opciones por rol.** El menú lateral y las tarjetas de accesos rápidos salen de `IPermisosService` +
  `Models/Core/Permissions/ModuloCatalogo.cs`. No hagas partials por rol (`_AccesosAdmin`, `_novedadesGlosador`…)
  ni `User.IsInRole("…")` para armar ligas: agrega el módulo al servicio y al catálogo. Lo que dependa del
  rol dentro de una página va como `bool` en su ViewModel (p. ej. `HomeViewModel.EsAdministrador`).
- **Mayúsculas y minúsculas.** En Docker (Linux), `~/css/vistas/Usuarios.css` ≠ `usuarios.css`, y lo mismo pasa con `<partial name>`.
  En Windows funciona y en el contenedor da 404 o "partial not found".
- **Carpetas.** El CSS de página nuevo va en `css/vistas/`. `css/componets/` (mal escrito) solo guarda
  `slideMenu.css`; no crees `css/componentes/` ni hojas por componente: los components no llevan CSS.
- **Un `<link>` a un archivo inexistente no falla en el build ni en el checker.** Lo atrapa
  `test/SIDAPA.presentation.test/Views/DashboardViewsTests.cs` (pide cada hoja de cada pantalla y espera 200).
  Si agregas una pantalla con CSS, súmala al `[Theory]` de esa prueba.
- **Razor codifica los acentos** (`Próximo` → `Pr&#xF3;ximo`). En pruebas que buscan texto en el HTML,
  decodifica primero con `WebUtility.HtmlDecode`.
- **Python no está instalado** en la máquina de desarrollo; por eso el checker es PowerShell
  (funciona con Windows PowerShell 5.1 y con pwsh). El script es ASCII a propósito: PS 5.1 lee
  un `.ps1` sin BOM como ANSI.

## Deuda conocida (al 2026-11-25)

El checker sale limpio con `-Strict`: **0 errores y 0 warnings**. Queda lo que el checker no mide:

- **Vistas sin `@model`** (regla 6): `Productos/Index` (pantalla por construir), `Auth/Denied` (estática),
  `Shared/_HomeAdmin` (datos de ejemplo escritos en el partial) y `Shared/_Sidebar` (parte del layout).
  `Usuarios` y `Reportes` reciben un `IEnumerable<…>` armado a mano en el controller, sin servicio ni `…ViewModel`.
- **`User.IsInRole` en `Views/SIDAPA/Index.cshtml`** (sección "Acerca de" solo para el Cliente): debería
  venir como `bool` en `InicioViewModel`.
- **`@media` en la portada** (`inicio-hero.css`): la rejilla del hero es propia; pasarla al grid de Bootstrap
  cuando diseño cierre la portada.
- **Estilos en línea `style="width: …%"`** en barras de avance: es el único modo sin JS de pasar un valor
  del servidor; se aceptan. `_HomeAdmin` además usa `style="height: 0.3rem"` porque un partial no puede traer CSS.

Fuera de front-end (no lo arregles aquí): 5 warnings CS8618 en `SIDAPA.domain/.../PersonEntity.cs`, y 6
warnings `CLOCK` del checker de pruebas (`DateTime.UtcNow` en `JwtTokenServiceTests` y `AuthCookieTests`).
