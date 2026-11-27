# Revisión del expediente (SCRUM-55 · HU-03)

**HU-03 · Revisar si el expediente está completo.** Como Capturador, quiero ver los documentos recibidos junto a la
lista de documentos que pide la operación, y marcar cada uno como **Correcto**, **Faltante** o **Con error**. El
Glosador y el Administrador también consultan los documentos.

## Pantallas

| Ruta | Qué hace | Política |
|---|---|---|
| `GET /Expedientes?q=&porRevisar=&pagina=` | Bandeja: referencias más recientes, 20 por página (una página fuera de rango muestra la última), búsqueda por número o razón social, filtro "Por revisar" (Recibida / En revisión) y Capturador asignado. | `Staff` |
| `GET /Expedientes/Detalle/{id}?documento=` | Documentos recibidos y lista de verificación lado a lado. Con `documento` se abre la vista previa. | `Staff` |
| `POST /Expedientes/Detalle/{id}` | Marca un documento (`DocumentoMarcado`, `Estado`, `Nota` opcional). | `Capture` + antiforgery |
| `GET /Expedientes/Archivo/{documentId}` | El archivo para el iframe de la vista previa (solo PDF y XML). | `Staff` |

El usuario que marca sale del claim `sub`, nunca del formulario. El Glosador ve la misma pantalla sin botones, y su
POST termina en `/Auth/Denied`.

## Reglas

- **Lista de verificación** (`cat_documento_requerido`, migraciones `SeedRequiredDocuments` e
  `SeedExportRequiredDocuments`), con cualquier clave de pedimento:
  - Obligatorios en importación y exportación: factura comercial / CFDI, lista de empaque y documento de transporte.
  - Opcionales en las dos: certificado de origen, ficha técnica, permisos / NOM, póliza de seguro y comprobantes de fletes.
  - Solo en importación (opcionales): padrón de importadores y manifestación de valor, que el catálogo marca
    "solo importación".
  - Los documentos 1–6 (constancia fiscal, acta…) son del expediente del cliente, no de cada operación.
  - El aviso de arribo todavía solo crea importaciones; la lista de exportación ya está lista para cuando se habilite.
- **Estado de cada renglón** (`ExpedienteRule.ChecklistStatus`): si no hay archivo, sale Faltante. Si hay varios
  archivos del mismo tipo, gana el mejor en este orden: Correcto > Recibido (por revisar) > Con error > Faltante.
  Un opcional sin archivo se pinta "No recibido" en gris.
- **Cuándo se marca** (`DocumentReviewRule`): solo en **Recibida** o **En revisión**. En cualquier otro estatus el
  detalle queda de solo consulta y el POST lanza `ExpedienteNotInReviewException`. `RECIBIDO` no es una marca válida.
- **Cambios de estatus** (`ExpedienteReviewService.MarkAsync`):
  1. El **primer documento marcado** pasa la referencia de Recibida a **En revisión**. Abrirla no cambia nada.
  2. Cuando todos los obligatorios tienen un archivo Correcto, `ExpedienteService.EvaluateAsync` (SCRUM-56) la pasa a
     **Expediente completo**, deja el registro en la bitácora y avisa al cliente.
- **Observación** (`ope_documento.nota`, máximo 300 caracteres, `DocumentReviewRule.NormalizeNote`): explica qué
  falta o qué está mal y se muestra bajo el documento. Se guarda sin espacios sobrantes; vacía queda en null; al marcar
  **Correcto** se borra, para que no quede el motivo de un rechazo anterior.
- **Capturador asignado** (`ope_referencia.id_capturador`): quien marca el primer documento se queda con la referencia;
  `AssignCapturadorAsync` solo escribe si estaba vacía, así que otro usuario no se la quita. Va en la misma transacción
  que la marca. Si el primero en marcar es un Administrador, la referencia queda a su nombre.
- **Concurrencia:** la marca va en una transacción con `SELECT … FOR UPDATE` sobre la referencia (el mismo candado que
  usa HU-05). `UpdateStatusAsync` filtra por documento **y** referencia, así que el documento de otra referencia da
  `DocumentNotFoundException`.

## Vista previa sin descargar

| Tipo | Cómo se ve |
|---|---|
| PDF | iframe con `/Expedientes/Archivo/{id}`, `Content-Disposition: inline`. |
| XML | El mismo iframe, pero el archivo va como **`text/plain`** con `Content-Security-Policy: sandbox`. Como `application/xml`, un XML con XHTML y `<script>` correría en el origen de SIDAPA (XSS con la sesión del personal). |
| XLS / XLSX | El servidor baja el archivo y lo convierte en tabla con **ExcelDataReader** (`ExcelSpreadsheetPreviewReader`). Muestra como máximo 10 hojas, 200 filas y 30 columnas, y avisa si recortó. Las celdas se escriben como texto: Razor las codifica. |

Todas las respuestas llevan `X-Content-Type-Options: nosniff` y `Cache-Control: private, no-store`. El diálogo es un
`modal show` que pinta el servidor y se cierra con una liga, sin JavaScript.

## Pruebas

| Proyecto | Clase |
|---|---|
| domain | `Rules/CaseFile/DocumentReviewRuleTests`, `ExpedienteChecklistTests`, `DocumentFileRuleTests` (`PreviewKindFor`) |
| application | `Services/CaseFile/Expediente/ExpedienteReviewServiceTests`: recorrido Recibida → En revisión → Expediente completo con el `ExpedienteService` real |
| infraestructure | `Repositories/CaseFile/Expediente/ExpedienteRepositoryTests` (PostgreSQL real: búsqueda, filtro, paginación, listas de importación y exportación, `UpdateStatusAsync` con observación, `AssignCapturadorAsync`) y `External/Spreadsheets/ExcelSpreadsheetPreviewReaderTests` (xlsx armado en memoria, topes) |
| presentation | `Controllers/CaseFile/Expediente/ExpedientesControllerTests` (roles, antiforgery, `sub` del token, cabeceras de `Archivo`), `Models/CaseFile/Expediente/ExpedienteDetalleViewModelTests`, `Views/DashboardViewsTests` |

## Fuera de alcance

- Pedirle al cliente el documento que falta o viene con error es HU-04 (`IDocumentRequestRepository.CreateAsync` ya existe).
- Reasignar una referencia a otro Capturador no tiene pantalla todavía.
