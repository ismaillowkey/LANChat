using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;

namespace LanChat.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private WifiManager.MulticastLock? _multicastLock;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

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
