using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.domain.Rules.Core;

public static class NotificationRule
{
    public const int MaxMessageLength = 500;


    public static string EnsureValidMessage(string? message)
    {
        var text = message?.Trim() ?? "";

        if(text.Length == 0) throw new EmptyNotificationMessageException();

        if(text.Length > MaxMessageLength) throw new NotificationMessageTooLongException(MaxMessageLength);

        return text;
    }
}