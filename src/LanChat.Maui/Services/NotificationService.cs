using System;

namespace LanChat.Maui.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message);
}

public static class NotificationService
{
    private static INotificationService? _instance;

    public static void Initialize(INotificationService instance)
    {
        _instance = instance;
    }

    public static void ShowNotification(string title, string message)
    {
        _instance?.ShowNotification(title, message);
    }
}
