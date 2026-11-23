# Aviso de arribo (HU-02 · SCRUM-46)

El Cliente crea un aviso de arribo, sube sus documentos (PDF, XML o Excel) indicando qué es cada uno y recibe un
número de referencia único. La referencia aparece en su panel con el estatus **Recibida**.

## Pantallas

| Ruta | Qué hace | Quién |
|---|---|---|
| `GET /Avisos` | Panel "Mis avisos de arribo": referencias del cliente con estatus, aduana y documentos. | Política `ClientPortal` (rol CLIENTE) |
| `GET /Avisos/Nuevo` | Formulario del aviso. | `ClientPortal` |
| `POST /Avisos/Nuevo` | Crea la referencia y sube los documentos; redirige al panel con el número asignado. | `ClientPortal` + antiforgery |

El cliente y el usuario salen del token (`client_id`, `sub`), nunca del formulario: un cliente solo ve y crea sus
propias referencias.

## Campos

| Campo | Regla |
|---|---|
| Tipo de operación* | Solo **Importación** por ahora (`ArrivalNoticeRule.AllowedOperationTypes`). |
| Fecha estimada de arribo* | Obligatoria. |
| Aduana de arribo* | Clave vigente del Apéndice 1 (`cat_aduana_seccion`). |
| Documentos* | Al menos 1; hasta 5 renglones. PDF/XML/XLS/XLSX, máximo 20 MB cada uno; se revisa también el contenido (firma del archivo), así que un `.exe` renombrado a `.pdf` se rechaza. Cada archivo lleva su tipo (`cat_tipo_documento`). |
| Observaciones | Opcional, máximo 500 caracteres. |

## Flujo (`ArrivalNoticeService.CreateAsync`)

1. Valida datos y **todos** los archivos antes de crear nada (un archivo inválido no deja una referencia a medias).
2. Toma el siguiente valor de la secuencia `sidapa.seq_numero_referencia` y arma el número `R-000123`
   (`ReferenciaNumberRule`). La secuencia evita números repetidos con avisos simultáneos (puede dejar huecos).
3. Inserta `ope_referencia` en estatus `RECIBIDA` y su primer registro en `ope_bitacora_estatus`.
4. Sube cada archivo al bucket con `DocumentService` (ruta `referencias/R-000123/<TIPO>/…`) y lo registra en `ope_documento`.
5. Si falla una subida, borra los archivos ya subidos y la referencia, y muestra el error.

Los errores de negocio son excepciones de dominio (`FileTypeNotAllowedException`, `FileTooLargeException`,
`NoDocumentsException`, `AduanaNotFoundException`…); `DomainExceptionFilter` vuelve a pintar el formulario con el mensaje.

## Límites de tamaño

- 20 MB por archivo (`DocumentFileRule.maxSizeInBytes`), validado en application.
- 110 MB por petición (`[RequestSizeLimit]` + `[RequestFormLimits]` en `AvisosController`): 5 archivos de 20 MB más los campos.

## Arrastrar y soltar

El formulario funciona sin JavaScript (POST normal). `wwwroot/js/avisos.js` agrega la zona para arrastrar y soltar,
que acomoda cada archivo en el primer renglón libre, y revisa extensión y tamaño en el navegador para avisar antes de
enviar. La validación que cuenta es la del servidor.

## Datos de prueba (solo Development)

`DatabaseSeeder` crea la empresa demo y el usuario `cliente@sidapa.local` con la contraseña de
`DevUsers:Password` (user-secrets).

## Pendientes

- **Apéndice 1:** la migración `AvisoArribo` siembra las aduanas (sección 0) **capturadas de memoria**. Hay que
  verificarlas contra el Anexo 22 publicado en el DOF y agregar las secciones aduaneras.
- Exportación y más de 5 documentos por aviso: fuera del alcance de HU-02.
