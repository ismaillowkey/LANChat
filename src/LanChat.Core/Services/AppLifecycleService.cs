using System;

namespace LanChat.Core.Services;

public static class AppLifecycleService
{
    public static bool IsForeground { get; set; } = true;

    public static Func<bool>? IsChatWindowVisible { get; set; }
}
