using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;

namespace LanChat.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationReplyReceiver : BroadcastReceiver
{
    public const string ActionReply = "com.CompanyName.LanChat.ACTION_REPLY";
    public const string ExtraPeerId = "extra_peer_id";
    public const string ExtraNotificationId = "extra_notification_id";
    public const string KeyTextReply = "key_text_reply";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null || intent.Action != ActionReply) return;

        var results = global::Android.App.RemoteInput.GetResultsFromIntent(intent);
        if (results == null) return;

        string? replyText = results.GetCharSequence(KeyTextReply)?.ToString();
        string? peerId = intent.GetStringExtra(ExtraPeerId);
        int notifId = intent.GetIntExtra(ExtraNotificationId, 0);

        if (!string.IsNullOrWhiteSpace(replyText) && !string.IsNullOrEmpty(peerId))
        {
            var manager = LanChat.App.MainViewModelInstance?.Manager;
            if (manager != null)
            {
                var target = manager.Peers.FirstOrDefault(p => p.Id == peerId);
                if (target != null)
                {
                    _ = manager.SendTextMessageAsync(replyText, target);
                }
            }

            // Dismiss notification after replying
            var notifManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            notifManager?.Cancel(notifId);
        }
    }
}
