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
| `Staff` | Capturador, Glosador, Administrador | Consulta de referencias del personal interno |
| `Capture` | Capturador, Administrador | HU-03, HU-04, HU-06 a HU-09, HU-11 a HU-17, HU-19 a HU-21 |
| `Glosa` | Glosador, Administrador | HU-10 catálogo de fracciones · HU-18 glosa |
| `Administration` | Administrador | HU-22 usuarios y roles · HU-23 tablero y reportes |

- El **Administrador** puede entrar a todo lo interno (no al portal del Cliente).
- El rol **Validador** existe en `cat_rol` pero todavía no tiene política: pendiente definir su alcance.
- Que un Cliente solo vea **sus** referencias (claim `client_id`) se valida en cada caso de uso,
  no con estas políticas.

## Cómo se prueba

Rutas de prueba (solo Development) en `DevPermissionsController`: `/dev/permisos/cliente`,
`/captura`, `/glosa`, `/admin`. Usuarios de prueba: `<rol>@sidapa.local` (contraseña en
user-secrets `DevUsers:Password`).

| Caso | Resultado esperado |
|---|---|
| Sin sesión → `/dev/permisos/glosa` | Redirige al login |
| Capturador → `/dev/permisos/captura` | Entra |
| Capturador → `/dev/permisos/glosa` | Acceso denegado |
| Administrador → `/captura`, `/glosa`, `/admin` | Entra |
| Administrador → `/cliente` | Acceso denegado |
| Sin sesión → `GET /api/dev/auth/me` | 401 |
