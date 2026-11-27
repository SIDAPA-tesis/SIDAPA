# Permisos por rol (SCRUM-42 · HU-01-T03)

El servidor decide qué rol puede usar cada parte del sistema; el menú (SCRUM-43) solo esconde
opciones, no protege nada por sí mismo.

## Reglas generales

- **Todo requiere sesión** salvo lo marcado con `[AllowAnonymous]` (login, acceso denegado,
  archivos estáticos). Una pantalla nueva sin atributo queda protegida por defecto.
- Sin sesión en una pantalla → redirige a `/Auth/Login?returnUrl=...`.
- Con sesión pero sin permiso → página **Acceso denegado** (`/Auth/Denied`).
- En `/api/...` no hay redirecciones: responde **401** (sin sesión) o **403** (sin permiso).
- Los nombres de rol salen de `RoleNames` (iguales a `cat_rol` y al claim `role` del token).
- Las políticas se definen en `SIDAPA.presentation/Security/AuthorizationSetup.cs` y se usan con
  `[Authorize(Policy = Policies.X)]` en el controlador o la acción.

## Matriz

| Política | Roles | Historias que cubre |
|---|---|---|
| `ClientPortal` | Cliente | HU-02 aviso de arribo · HU-05 subir documento faltante · HU-24 mis operaciones |
| `Staff` | Capturador, Glosador, Administrador | Consulta de referencias del personal interno · HU-03 consulta del expediente |
| `Capture` | Capturador, Administrador | HU-03, HU-04, HU-06 a HU-09, HU-11 a HU-17, HU-19 a HU-21 |
| `Glosa` | Glosador, Administrador | HU-10 catálogo de fracciones · HU-18 glosa |
| `Administration` | Administrador | HU-22 usuarios y roles · HU-23 tablero y reportes |

- El **Administrador** puede entrar a todo lo interno (no al portal del Cliente).
- El rol **Validador** existe en `cat_rol` pero todavía no tiene política: pendiente definir su alcance.
- Que un Cliente solo vea **sus** referencias (claim `client_id`) se valida en cada caso de uso,
  no con estas políticas.

## Pantallas y menú lateral

| Pantalla | Política | Ven la opción en el menú |
|---|---|---|
| `/Home` (Inicio) | sesión | Todos |
| `/Expedientes` (consulta y vista previa) | `Staff` | Capturador, Glosador, Administrador |
| `POST /Expedientes/Detalle/{id}` (marcar documentos) | `Capture` | — (los botones solo salen al Capturador y al Administrador) |
| `/Pedimentos` | `Staff` | Capturador, Glosador, Administrador |
| `/Claves`, `/Productos` | `Glosa` | Glosador, Administrador |
| `/Usuarios`, `/Reportes` | `Administration` | Administrador |
| `/Avisos`, `/Solicitudes` | `ClientPortal` | Cliente |

- Qué módulos ve cada rol lo decide **una sola clase**: `PermisosService` (`SIDAPA.application/Services/Core/`).
  El menú lateral (`MenuLateralViewComponent`) y las tarjetas de accesos rápidos (`AccesosRapidosViewComponent`)
  la usan; el texto y la liga de cada módulo están en `Models/Core/Permissions/ModuloCatalogo.cs`.
- Un módulo nuevo: valor en `ModuleEnum`, roles en `PermisosService`, entrada en `ModuloCatalogo` y
  `[Authorize(Policy = …)]` en su controller. Si falta el atributo, la opción se esconde pero la URL sigue abierta.

## Cómo se prueba

Con pruebas automáticas en `test/SIDAPA.presentation.test/Security/` (app completa en memoria con
`SidapaWebFactory`), que reemplazan las antiguas rutas manuales `/dev/permisos/*` y `/api/dev/auth/*`:

- `AuthorizationPolicyTests`: la matriz de arriba completa, **cada política × cada rol** (incluido
  Validador, que no entra a nada), con las políticas reales de `AddSidapaAuthorization`; y que el rol
  distingue mayúsculas (`glosador` no entra).
- `AuthCookieTests`: sin sesión → `/Auth/Login?returnUrl=…`; Capturador en `/Claves` → `/Auth/Denied`;
  token vencido o firmado con otra llave → sin sesión; token a media vida → se entrega cookie nueva
  (HttpOnly, Secure, SameSite=Strict).
- `DashboardRoutesTests`: `/Usuarios`, `/Reportes` y `/Productos` mandan a `/Auth/Denied` a los roles sin permiso y
  dejan entrar a los que sí; el menú lateral del Glosador solo tiene sus módulos y marca la pantalla actual.
- `PermisosServiceTests` (application) y `ModuloCatalogoTests` (presentation): módulos por rol y cómo se pintan.

```bash
dotnet test --project test/SIDAPA.presentation.test
```

Para probar a mano siguen existiendo los usuarios `<rol>@sidapa.local` (contraseña en user-secrets
`DevUsers:Password`, solo Development).
