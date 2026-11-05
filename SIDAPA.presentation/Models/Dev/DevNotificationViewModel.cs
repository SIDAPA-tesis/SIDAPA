using SIDAPA.application.DTOs.Core.Notifications;

namespace SIDAPA.presentation.Models.Dev;

public class DevNotificationViewModel
{
    public string? Message { get; set; }
    public bool SendEmail { get; set; } = true;
    public int UnreadCount { get; set; }
    public IReadOnlyList<NotificationDto> Notifications { get; set; } = [];
}