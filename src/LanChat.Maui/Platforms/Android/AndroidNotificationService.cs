using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using LanChat.Maui.Services;

namespace LanChat.Maui.Platforms.Android;

public class AndroidNotificationService : INotificationService
{
    private const string ChannelId = "lanchat_channel";
    private const string ChannelName = "LAN Chat Messages";
    private static int _notificationId = 1000;

    public AndroidNotificationService()
    {
        CreateChannel();
    }

    private void CreateChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
            {
                Description = "Incoming LAN Chat Messages"
            };
            channel.EnableVibration(true);
            channel.EnableLights(true);

            var notificationManager = (NotificationManager?)global::Android.App.Application.Context.GetSystemService(Context.NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetAutoCancel(true)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetDefaults((int)(NotificationDefaults.Sound | NotificationDefaults.Vibrate))
                .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Notify(System.Threading.Interlocked.Increment(ref _notificationId), builder.Build());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
        }
    }
}
