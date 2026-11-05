namespace SIDAPA.application.DTOs.Core.Notifications;

public record NotificationDto(int Id, string Message, int? ReferenciaId, bool IsRead, DateTime CreatedAt);