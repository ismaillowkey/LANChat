using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
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
        if (Window != null)
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            Window.SetSoftInputMode(SoftInput.AdjustResize);

            ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, new KeyboardInsetsListener());

            var contentView = Window.DecorView.FindViewById(Android.Resource.Id.Content);
            if (contentView != null)
            {
                contentView.ViewTreeObserver?.AddOnGlobalLayoutListener(new GlobalLayoutListener(contentView));
            }
        }

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

public class KeyboardInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
    {
        try
        {
            var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            var navInsets = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());
            int keyboardHeightPx = Math.Max(0, imeInsets.Bottom - navInsets.Bottom);

            if (imeInsets.Bottom > 0)
            {
                KeyboardHelper.NotifyKeyboardHeight(keyboardHeightPx);
            }
            else
            {
                KeyboardHelper.NotifyKeyboardHeight(0);
            }
        }
        catch { }
        return ViewCompat.OnApplyWindowInsets(v, insets);
    }
}

public class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
{
    private readonly Android.Views.View _contentView;

    public GlobalLayoutListener(Android.Views.View contentView)
    {
        _contentView = contentView;
    }

    public void OnGlobalLayout()
    {
        try
        {
            var r = new Android.Graphics.Rect();
            _contentView.GetWindowVisibleDisplayFrame(r);
            int screenHeight = _contentView.RootView?.Height ?? 0;
            if (screenHeight <= 0) return;

            int heightDiff = screenHeight - r.Bottom;
            // Keypad is open if keypadHeight > 15% of screen height
            if (heightDiff > screenHeight * 0.15)
            {
                KeyboardHelper.NotifyKeyboardHeight(heightDiff);
            }
            else
            {
                KeyboardHelper.NotifyKeyboardHeight(0);
            }
        }
        catch { }
    }
}
