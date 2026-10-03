using System;
using Microsoft.Maui.Devices;

namespace LanChat.Maui.Services;

public static class KeyboardHelper
{
    public static event Action<double>? KeyboardHeightChanged;
    private static double _lastHeight = -1;

    public static void NotifyKeyboardHeight(int heightPx)
    {
        double density = DeviceDisplay.MainDisplayInfo.Density;
        double heightDp = heightPx / (density > 0 ? density : 1.0);

        if (Math.Abs(heightDp - _lastHeight) > 3.0)
        {
            _lastHeight = heightDp;
            KeyboardHeightChanged?.Invoke(heightDp);
        }
    }
}
