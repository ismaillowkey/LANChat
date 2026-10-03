using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using LanChat.Maui.Platforms.Android;
using LanChat.Maui.Services;

namespace LanChat.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    LaunchMode = LaunchMode.SingleTop, 
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : MauiAppCompatActivity
{
    private WifiManager.MulticastLock? _multicastLock;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Ensure soft keyboard resizes the view, keeping header fixed at the top
        Window?.SetSoftInputMode(SoftInput.AdjustResize);

        // Initialize Android notification service
        LanChat.Maui.Services.NotificationService.Initialize(new AndroidNotificationService());

        // Request notification permission on Android 13+ (API 33+)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            {
                RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 101);
            }
        }

        try
        {
            var wifiManager = (WifiManager?)GetSystemService(WifiService);
            if (wifiManager != null)
            {
                _multicastLock = wifiManager.CreateMulticastLock("LanChatMulticastLock");
                _multicastLock?.SetReferenceCounted(true);
                _multicastLock?.Acquire();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error acquiring MulticastLock: {ex.Message}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        try
        {
            _multicastLock?.Release();
            _multicastLock = null;
        }
        catch { }
    }
}
