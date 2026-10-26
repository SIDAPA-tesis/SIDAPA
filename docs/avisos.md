# Servicio de avisos (SCRUM-59 · HU-04-T02)

Cualquier parte del sistema avisa a un usuario o cliente con `INotificationService`.
El aviso se guarda en `ope_notificacion` (panel) y, si `sendEmail` es true, se manda por correo.

```csharp
// En un servicio de application (inyectar INotificationService):
await notifications.NotifyClientAsync(
    referencia.ClientId,
    $"Faltan documentos de la referencia {referencia.ReferenciaNumber}:\n- B/L\n- Lista de empaque",
    referencia.Id);
```

| Método | Para qué |
|---|---|
| `NotifyUserAsync` | Aviso a un usuario (p. ej. al Capturador asignado) |
| `NotifyClientAsync` | Aviso a todos los usuarios activos de una empresa cliente |
| `GetRecentAsync` / `CountUnreadAsync` | Panel y contador de avisos |
| `MarkAsReadAsync` | Marcar como leído (solo avisos propios) |

- Mensaje: obligatorio, máximo 500 caracteres.
- Si el correo falla, el aviso del panel se guarda igual (`enviada_correo = false`).
- Sin servidor configurado (`Email:Host` vacío) el correo solo se escribe en el log.

## Configurar correo real (opcional)
```powershell
dotnet user-secrets set "Email:Host" "smtp.gmail.com" --project SIDAPA.presentation
dotnet user-secrets set "Email:From" "<correo>" --project SIDAPA.presentation
dotnet user-secrets set "Email:User" "<correo>" --project SIDAPA.presentation
dotnet user-secrets set "Email:Password" "<contraseña de aplicación>" --project SIDAPA.presentation
```

Prueba (solo Development): `/dev/avisos`.