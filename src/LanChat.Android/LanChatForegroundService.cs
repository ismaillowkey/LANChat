using System;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;

namespace LanChat.Android;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
public class LanChatForegroundService : Service
{
    public const string ChannelId = "lanchat_service_channel";
    public const int ServiceNotificationId = 9999;
    private WifiManager.MulticastLock? _multicastLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateServiceNotificationChannel();
        AcquireMulticastLock();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var notification = BuildServiceNotification();

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(ServiceNotificationId, notification, global::Android.Content.PM.ForegroundService.TypeConnectedDevice);
            }
            else
            {
                StartForeground(ServiceNotificationId, notification);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting foreground service: {ex.Message}");
        }

        return StartCommandResult.Sticky;
    }

    private void AcquireMulticastLock()
    {
        try
        {
            var wifi = (WifiManager?)GetSystemService(WifiService);
            if (wifi != null)
            {
                _multicastLock = wifi.CreateMulticastLock("LanChatMulticastLock");
                _multicastLock?.SetReferenceCounted(true);
                _multicastLock?.Acquire();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Multicast lock error: {ex.Message}");
        }
    }

    private void CreateServiceNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Layanan Latar Belakang LAN Chat", NotificationImportance.Low)
            {
                Description = "Menjaga koneksi socket tetap aktif untuk menerima pesan LAN di background"
            };
            channel.EnableVibration(false);
            channel.EnableLights(false);

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }

    private Notification BuildServiceNotification()
    {
        var launchIntent = new Intent(this, typeof(MainActivity));
        launchIntent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        int iconRes = ApplicationInfo?.Icon != 0 ? ApplicationInfo.Icon : global::Android.Resource.Drawable.SymDefAppIcon;

        Notification.Builder builder;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            builder = new Notification.Builder(this, ChannelId);
        }
        else
        {
#pragma warning disable CS0618
            builder = new Notification.Builder(this);
#pragma warning restore CS0618
        }

        builder.SetContentTitle("LAN Chat")
               .SetContentText("Aktif di latar belakang untuk menerima pesan")
               .SetSmallIcon(iconRes)
               .SetOngoing(true)
               .SetContentIntent(pendingIntent);

        return builder.Build();
    }

    public override void OnDestroy()
    {
        try
        {
            if (_multicastLock != null && _multicastLock.IsHeld)
            {
                _multicastLock.Release();
            }
        }
        catch { }

        base.OnDestroy();
    }
}
