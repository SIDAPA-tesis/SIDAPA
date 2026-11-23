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
Responsividad con las clases de Bootstrap, no con `@media`.

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
datos por `ViewBag`/`ViewData` (salvo `Title`), ni se pone lógica de negocio en la vista.

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
  **servicio o interfaz de application** (como `MenuLateralViewComponet` con `IPermisosService`),
  **nunca `SidapaDbContext`**, y su vista va en `Views/Shared/Components/<Nombre>/Default.cshtml`.
- **Nunca dentro de un component**: `<link>`, `<script>`, `@section`. Sus estilos van en el CSS
  de la página o del layout, y su JS en el `.js` de la página.
- **Fuera de los components**: lo que no se repite (encabezado de la página, formulario de filtros único).

## Receta: página nueva

1. `Models/<Area>/<X>ViewModel.cs`: un `record` con lo que la vista muestra, y un `Desde(...)` si se arma desde un DTO.
2. Una acción en `Controllers/Web/<Area>/<X>Controller.cs` que llama al servicio y regresa `View(viewModel)`.
3. `Views/<X>/Index.cshtml` con `@model`, `ViewData["Title"]`, `Layout` y el maquetado con Bootstrap.
4. Un partial `_<Elemento>.cshtml` por cada cosa que se repite en un `@foreach`.
5. Si hace falta, un solo `wwwroot/css/vistas/<x>.css` en `@section Styles`.
6. Correr el checker con `-Include` sobre los archivos tocados → exit 0. Después correr `dotnet build SIDAPA.sln`.

## Gotchas (verificados en el repo)

- **`Styles` solo lo renderiza `_AuthLayout`.** `_dashboardLayout` e `_inicioLayout` no tienen
  `RenderSectionAsync("Styles")`, y `Pedimentos`/`Claves` declaran `@section Estilos`, que ningún
  layout renderiza. Antes de poner CSS de página en una vista que usa esos layouts, agrega
  `@await RenderSectionAsync("Styles", required: false)` en el `<head>` del layout, **después** de
  Bootstrap y `theme.css`.
- **Convención de nombres de ViewComponents.** MVC quita el sufijo exacto `ViewComponent` y busca la vista en
  `Views/Shared/Components/<Nombre>/Default.cshtml`. La clase existente `MenuLateralViewComponet`
  (sufijo mal escrito) se registra como `MenuLateralViewComponet`, y su vista está en
  `Views/Shared/Componets/`, una carpeta que MVC no revisa. En components nuevos usa la ortografía
  correcta; la carpeta de clases `ViewComponets/` se queda como está.
- **Mayúsculas y minúsculas.** En Docker (Linux), `~/css/vistas/Usuarios.css` ≠ `usuarios.css`, y lo mismo pasa con `<partial name>`.
  En Windows funciona y en el contenedor da 404 o "partial not found".
- **Carpetas parecidas.** Ya existen `css/componets/` (con archivos) y referencias a `css/componentes/` (que no
  existe en disco). El CSS de página nuevo va en `css/vistas/`.
- **Python no está instalado** en la máquina de desarrollo; por eso el checker es PowerShell
  (funciona con Windows PowerShell 5.1 y con pwsh). El script es ASCII a propósito: PS 5.1 lee
  un `.ps1` sin BOM como ANSI.

## Deuda conocida (al 2026-11-23, `-NoWarnings`)

```
CSS-PER-PAGE  Views/Claves/Index.cshtml        2 CSS
CSS-PER-PAGE  Views/Pedimentos/Detalle.cshtml  2 CSS
CSS-PER-PAGE  Views/Pedimentos/Index.cshtml    4 CSS
CSS-LINES     wwwroot/css/componets/slideMenu.css          344 líneas
CSS-LINES     wwwroot/css/layout/adminPanel/adminPanel.css 423 líneas
```
Además hay unos 230 warnings (px y colores hex en `app.css`, `variables.css`, `slideMenu.css` y
`inicio-*.css`, y un `onclick` en `_inicioLayout`). Se atienden en tareas SCRUM aparte. No los
arregles de paso dentro de otra tarea.
