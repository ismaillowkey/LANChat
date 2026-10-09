using System;

namespace LanChat.Core.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message, string? peerId = null);
}

public static class NotificationManagerService
{
    public static INotificationService? Instance { get; set; }

    public static void Show(string title, string message, string? peerId = null)
    {
        try
        {
            Instance?.ShowNotification(title, message, peerId);
        }
        catch { }
    }
}
