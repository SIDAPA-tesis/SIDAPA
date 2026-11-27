# Documento solicitado: subida del cliente y aviso al Capturador (SCRUM-64 · HU-05)

**HU-05 · Subir el documento que me piden.** Como Cliente, quiero que el sistema me avise qué documento falta y me dé
un botón para subir solo ese archivo. Esta tarea cubre la parte técnica: subir el archivo, agregarlo al expediente,
regresar la referencia a **En revisión** y avisar al Capturador.

## Flujo

1. El Capturador pide un documento (HU-04, **pendiente**). Eso crea una fila en `ope_solicitud_documento` y la
   referencia pasa a **En espera de documentos**.
2. En **Mis avisos de arribo** el cliente ve la tarjeta **"Documentos que te pedimos"**
   (`SolicitudesPendientesViewComponent`). Cada documento aparece con la nota del Capturador y un botón **Subir**.
3. **Subir** abre `GET /Solicitudes/Subir?requestId={id}`. La página tiene un solo campo de archivo; el tipo de
   documento no se elige, porque lo fija la solicitud.
4. `POST /Solicitudes/Subir` llama a `DocumentRequestService.AttendAsync`:

   | Paso | Dónde | Si falla |
   |---|---|---|
   | Busca la solicitud **junto con el `client_id` del token** | `GetForClientAsync` | `DocumentRequestNotFoundException` (otra empresa no puede ni verla) |
   | Revisa que siga abierta y que la referencia esté En espera | `DocumentRequestRule.EnsureCanAttend` | `DocumentRequestAlreadyAttendedException` / `ReferenciaNotAwaitingDocumentsException` |
   | Valida el archivo y lo sube al bucket | `DocumentService.StoreFileAsync` (fuera de la transacción) | Errores de archivo (SCRUM-50); no se sube nada |
   | **Transacción** (`IUnitOfWork`): `SELECT … FOR UPDATE` de la referencia → inserta `ope_documento` (RECIBIDO) → cierra la solicitud con `UPDATE … WHERE fecha_atencion IS NULL` → si ya no quedan abiertas, `En espera → En revisión` + bitácora | repositorios | Rollback + se borra el archivo del bucket |
   | Aviso al Capturador que pidió el documento (`RequestedByUserId`) | `INotificationService.NotifyUserAsync` (panel + correo) | El correo nunca lanza |

5. El cliente regresa a **Mis avisos** con la confirmación: cuántos documentos faltan, o que la referencia regresó a revisión.

## Reglas de negocio

- La referencia regresa a **En revisión** solo cuando el cliente atiende la **última** solicitud abierta. Con cada
  subida el Capturador recibe un aviso: *"El cliente subió "Lista de empaque" de la referencia R-000123. Falta 1
  documento por recibir."* o *"… Ya no hay documentos pendientes: la referencia regresó a En revisión."*
- Se avisa a quien pidió el documento (`ope_solicitud_documento.id_capturador`). `ope_referencia.id_capturador`
  lo asigna la revisión del expediente (HU-03, `docs/revision-de-expediente.md`) a quien marca el primer documento.
- El documento entra como **RECIBIDO**. El Capturador lo revisa; si también viene mal, la idea es pedirlo de nuevo con
  otra solicitud (HU-04). El documento rechazado anterior se conserva en el expediente.
- El documento nuevo no cuenta para **Expediente completo** (SCRUM-56) hasta que el Capturador lo marque CORRECTO.

## Concurrencia

- **Doble clic / doble envío:** el `UPDATE` condicional deja ganar solo al primero. El segundo recibe
  "Ya recibimos este documento" y su archivo se borra del bucket.
- **Dos documentos distintos subidos al mismo tiempo:** sin candado, cada subida vería abierta la solicitud de la otra
  (todavía sin confirmar), ninguna cambiaría el estatus y la referencia se quedaría en espera para siempre. El
  `FOR UPDATE` sobre la referencia hace que la segunda espere a la primera. Lo prueba
  `DocumentRequestRepositoryTests.DosSubidasAlMismoTiempo_ConCandado_…` con PostgreSQL real; sin el candado, esa prueba falla.
- `ChangeStatusAsync` se une a la transacción abierta si existe (EF no permite anidar transacciones).

## Probarlo a mano (Development)

Con `DEV_USERS_PASSWORD` definido, el seed crea la referencia **`DEMO-HU05`** de la empresa demo, en **En espera de
documentos**, con dos solicitudes abiertas (B/L y lista de empaque) de `capturador@sidapa.local`. El seed solo la crea
si no existe; para repetir la demo, bórrala.

1. Entra como `cliente@sidapa.local` → **Mis avisos de arribo** → tarjeta con 2 documentos.
2. Sube el primero: la referencia sigue En espera y la confirmación dice que falta 1.
3. Sube el segundo: la referencia pasa a **En revisión**.
4. Revisa `sidapa.ope_notificacion` (avisos del capturador) y `sidapa.ope_bitacora_estatus`.

## Pendiente / fuera de alcance

- **HU-04** (el Capturador pide el documento): `IDocumentRequestRepository.CreateAsync` ya existe para su servicio.
- **Bandeja de avisos en pantalla:** hoy el Capturador recibe el aviso como fila en `ope_notificacion` y por correo;
  todavía no hay pantalla que la muestre.
- Si el proceso se cae entre la subida al bucket y el commit, el archivo queda huérfano (igual que en SCRUM-48).

## Pruebas

| Proyecto | Clase |
|---|---|
| domain | `Rules/CaseFile/DocumentRequestRuleTests` |
| application | `Services/CaseFile/Documents/DocumentRequestServiceTests`, `DocumentServiceTests` (`StoreFileAsync`) |
| infraestructure | `Repositories/CaseFile/Documents/DocumentRequestRepositoryTests` (Testcontainers: cierre condicional, aislamiento por cliente, rollback, carrera) |
| presentation | `Controllers/CaseFile/Documents/SolicitudesControllerTests`, `Models/CaseFile/Documents/SolicitudesViewModelTests` |
