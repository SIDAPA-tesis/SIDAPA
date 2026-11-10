namespace SIDAPA.application.DTOs.Core.Notifications;

public record EmailMessageDto(string To, string Subject, string Body);