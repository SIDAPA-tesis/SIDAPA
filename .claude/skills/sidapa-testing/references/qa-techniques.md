# Técnicas de QA para diseñar casos (con ejemplos de SIDAPA)

Úsalas para pasar de "probé que funciona" a "probé dónde se rompe". No hace falta usarlas todas: elige
las que tocan el código que pruebas y explica en el reporte qué dejaste fuera.

## Particiones de equivalencia
Divide las entradas en grupos que el código trata igual y toma **un** representante de cada uno.

Ejemplo — extensión de un documento (`DocumentFileRule`):

| Partición | Representante | Resultado |
|---|---|---|
| Permitida, contenido correcto | `factura.pdf` con `%PDF-` | válido |
| Permitida, mayúsculas | `FACTURA.PDF` | válido (se compara en minúsculas) |
| Permitida, contenido de otro tipo | `bl.pdf` con bytes `MZ` | `FileContentMismatchException` |
| No permitida | `setup.exe` | `FileTypeNotAllowedException` |
| Sin extensión | `factura` | `FileTypeNotAllowedException` |
| Doble extensión | `factura.pdf.exe` | `FileTypeNotAllowedException` |

## Valores límite
Los errores viven en los bordes. Por cada tope prueba **límite − 1, límite, límite + 1** (al menos límite
y límite + 1) y el cero.

| Regla | Casos |
|---|---|
| Tamaño máximo 20 MB (`DocumentFileRule.maxSizeInBytes`) | `0` → vacío; `20 MB` → pasa; `20 MB + 1` → `FileTooLargeException` |
| Observaciones ≤ 500 (`ArrivalNoticeRule.MaxNotesLength`) | 500 pasa; 501 lanza; solo espacios → se guarda `null` |
| Mensaje de aviso ≤ 500 (`NotificationRule`) | igual, y además el recorte (`"  x  "` cuenta 1) |
| Folio `R-000001` (6 dígitos) | 1, 999 999, 1 000 000 (crece, no se trunca) |
| Encabezado de 8 bytes (`HeaderLength`) | archivo de menos de 8 bytes con firma válida |

Usa la **constante** del dominio en la prueba (`DocumentFileRule.maxSizeInBytes + 1`), no el número
copiado: si el negocio cambia el límite, la prueba sigue probando el borde.

## Tablas de decisión
Cuando el resultado depende de varias condiciones a la vez, enumera las combinaciones.

`ExpedienteService.EvaluateAsync`:

| ¿Completo? | ¿Estatus = EN_REVISION? | ¿El repo aceptó el cambio? | Resultado esperado |
|---|---|---|---|
| no | — | — | no cambia, no avisa |
| sí | no | — | no cambia, no avisa |
| sí | sí | no (otro usuario ganó) | no cambia, **no avisa dos veces** |
| sí | sí | sí | cambia a EXPEDIENTE_COMPLETO y avisa al cliente una vez |

Permisos (`AuthorizationSetup`): política × rol completa, incluido el rol que no tiene política
(`VALIDADOR`) y el rol con mayúsculas distintas.

## Transiciones de estado
`ReferenciaStatusEnum` + `ReferenciaStatusFlow` es una máquina de estados. Prueba:
- cada transición **permitida** (al menos las que usa el código que pruebas);
- transiciones **prohibidas** representativas (saltar estados, regresar de CERRADO);
- la transición al **mismo** estado;
- qué hace el servicio si la referencia ya no está en el estado de origen cuando escribe (concurrencia).

## Compensación y atomicidad
En flujos de varios pasos (aviso de arribo: referencia → N documentos → bucket):
- falla el paso 1 → no hay nada guardado;
- falla el paso k → se deshicieron los pasos 1..k−1 (archivos borrados del bucket, referencia borrada);
- falla también la compensación → se propaga el error **original**, no el de la compensación;
- validación antes de escribir: un archivo inválido en la posición N no deja escrito nada de los anteriores.

## Seguridad y datos de otro cliente
- Sin sesión → login (pantallas) / 401 (`/api`).
- Rol sin permiso → `/Auth/Denied` (pantallas) / 403 (`/api`).
- Usuario Cliente pidiendo una referencia de **otro** `client_id` → no debe verla (esto lo valida el
  caso de uso, no la política: pruébalo en el servicio).
- Token vencido, firmado con otra llave, o sin `role` → sin sesión.

## Fechas y tiempo
- Las columnas son `timestamp without time zone`: lo que se guarda debe ser UTC con `Kind = Unspecified`.
- Inyecta `TimeProvider` y fija el reloj; afirma la fecha **exacta**, no "parecida a ahora".
- Casos de borde: fin de mes, cambio de año, fecha de arribo en el pasado.

## Errores de infraestructura
- BD lanza al guardar → ¿se limpia lo que ya se subió al bucket?
- El reader responde 4xx/5xx o JSON vacío → excepción clara con el nombre del archivo.
- El correo falla → el aviso en panel sí queda guardado y `IsEmailSent` sigue en `false`.

## Checklist rápido antes de dar por terminado
- [ ] Camino feliz con todas las salidas observables afirmadas (lo regresado **y** lo guardado).
- [ ] Una prueba por cada excepción de dominio que el método puede lanzar.
- [ ] Límites con la constante del dominio.
- [ ] Qué **no** debe ocurrir cuando falla (nada guardado, nada enviado).
- [ ] Reloj controlado si hay fechas.
- [ ] Riesgos no cubiertos listados en el reporte.
