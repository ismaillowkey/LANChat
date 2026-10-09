using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using LanChat.Core.Services;

namespace LanChat.Android;

public class AndroidNotificationService : INotificationService
{
    private const string ChannelId = "lanchat_messages";
    private const string ChannelName = "Pesan LAN Chat";
    private readonly Context _context;
    private static int _notificationId = 1000;

    public AndroidNotificationService(Context context)
    {
        _context = context;
        CreateNotificationChannel();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
            {
                Description = "Notifikasi pesan baru LAN Chat"
            };
            channel.EnableLights(true);
            channel.EnableVibration(true);

            var manager = (NotificationManager?)_context.GetSystemService(Context.NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }

    public void ShowNotification(string title, string message, string? peerId = null)
    {
        try
        {
            var intent = new Intent(_context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                _context,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            Notification.Builder builder;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder = new Notification.Builder(_context, ChannelId);
            }
            else
            {
#pragma warning disable CS0618
                builder = new Notification.Builder(_context);
#pragma warning restore CS0618
            }

            int iconRes = _context.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.SymDefAppIcon;
            if (iconRes == 0) iconRes = global::Android.Resource.Drawable.SymDefAppIcon;

            builder.SetContentTitle(title)
                   .SetContentText(message)
                   .SetSmallIcon(iconRes)
                   .SetAutoCancel(true)
                   .SetContentIntent(pendingIntent);

            int notifId = Interlocked.Increment(ref _notificationId);

            // Add Quick Reply Action if this is a direct message from a peer
            if (!string.IsNullOrEmpty(peerId) && peerId != LanChat.Core.Models.DevicePeer.BroadcastTargetId)
            {
                var replyIntent = new Intent(_context, typeof(NotificationReplyReceiver));
                replyIntent.SetAction(NotificationReplyReceiver.ActionReply);
                replyIntent.PutExtra(NotificationReplyReceiver.ExtraPeerId, peerId);
                replyIntent.PutExtra(NotificationReplyReceiver.ExtraNotificationId, notifId);

                var flags = PendingIntentFlags.UpdateCurrent;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    flags |= PendingIntentFlags.Mutable;
                }

                var replyPendingIntent = PendingIntent.GetBroadcast(_context, notifId, replyIntent, flags);

                var remoteInput = new global::Android.App.RemoteInput.Builder(NotificationReplyReceiver.KeyTextReply)
                    .SetLabel("Balas...")
                    .Build();

                var replyAction = new Notification.Action.Builder(
                    iconRes,
                    "Balas",
                    replyPendingIntent)
                    .AddRemoteInput(remoteInput)
                    .Build();

                builder.AddAction(replyAction);
            }

            var notification = builder.Build();
            var manager = (NotificationManager?)_context.GetSystemService(Context.NotificationService);
            manager?.Notify(notifId, notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing Android notification: {ex.Message}");
        }
    }
}
