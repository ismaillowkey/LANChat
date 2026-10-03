using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanChat.Core.Models;
using LanChat.Core.Services;

namespace LanChat.Maui.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
    public LanChatManager Manager { get; }

    public LocalizationService Strings => LocalizationService.Instance;

    public ObservableCollection<ChatMessage> FilteredMessages { get; } = new();

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private bool _hasAnyUnread;

    public event Func<Task>? RequestOpenDrawer;
    public event Func<Task>? RequestCloseDrawer;

    public string CurrentChatModeBadgeText => SelectedTarget?.IsBroadcastTarget == true
        ? Strings.ModeBroadcastBadge
        : Strings.ModePrivateBadge;

    public MainChatViewModel()
    {
        Manager = new LanChatManager(DeviceInfo.Current.Name);
        DeviceName = Manager.LocalDeviceName;

        // Apply initial target (Broadcast)
        ApplyFilter();

        Manager.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Manager.SelectedTarget))
            {
                OnPropertyChanged(nameof(SelectedTarget));
                OnPropertyChanged(nameof(CurrentChatModeBadgeText));
                ApplyFilter();
            }
        };

        Manager.Messages.CollectionChanged += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyFilter();
            });
        };

        Manager.MessageReceived += OnIncomingMessage;

        Strings.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Strings));
            OnPropertyChanged(nameof(CurrentChatModeBadgeText));
            foreach (var msg in Manager.Messages)
            {
                if (msg.IsImage || msg.IsVideo)
                {
                    msg.UpdateMediaState();
                }
            }
            ApplyFilter();
        };

        Manager.Start();
    }

    public DevicePeer SelectedTarget
    {
        get => Manager.SelectedTarget;
        set
        {
            if (Manager.SelectedTarget != value && value != null)
            {
                Manager.SelectedTarget = value;
                value.HasUnread = false;
                value.UnreadCount = 0;
                HasAnyUnread = Manager.Peers.Any(p => p.HasUnread);

                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentChatModeBadgeText));
                ApplyFilter();
            }
        }
    }

    public void ApplyFilter()
    {
        var target = SelectedTarget;
        bool isBroadcast = target == null || target.IsBroadcastTarget;

        var matching = Manager.Messages.Where(msg =>
        {
            if (isBroadcast)
            {
                return !msg.IsDirect;
            }
            else
            {
                return msg.IsDirect && (msg.SenderId == target!.Id || msg.TargetId == target!.Id);
            }
        }).ToList();

        // Sync FilteredMessages
        FilteredMessages.Clear();
        foreach (var m in matching)
        {
            FilteredMessages.Add(m);
        }
    }

    private void OnIncomingMessage(ChatMessage msg)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (msg.IsDirect)
            {
                var senderPeer = Manager.Peers.FirstOrDefault(p => p.Id == msg.SenderId);
                if (senderPeer != null)
                {
                    if (SelectedTarget == null || SelectedTarget.Id != senderPeer.Id)
                    {
                        senderPeer.HasUnread = true;
                        senderPeer.UnreadCount++;
                        HasAnyUnread = true;
                    }
                }
            }
            else
            {
                var broadcastPeer = Manager.Peers.FirstOrDefault(p => p.IsBroadcastTarget);
                if (broadcastPeer != null && (SelectedTarget == null || !SelectedTarget.IsBroadcastTarget))
                {
                    broadcastPeer.HasUnread = true;
                    broadcastPeer.UnreadCount++;
                    HasAnyUnread = true;
                }
            }

            // Trigger Android System Notification with sound/vibrate
            if (!msg.IsOutgoing)
            {
                string notifText = msg.IsImage
                    ? Strings.FormatUserSentImage(msg.SenderName)
                    : msg.IsVideo
                        ? Strings.FormatUserSentVideo(msg.SenderName)
                        : msg.Content;

                Services.NotificationService.ShowNotification(msg.SenderName, notifText);
            }

            ApplyFilter();
        });
    }

    [RelayCommand]
    private async Task OpenDrawerAsync()
    {
        if (RequestOpenDrawer != null)
        {
            await RequestOpenDrawer.Invoke();
        }
    }

    [RelayCommand]
    private async Task CloseDrawerAsync()
    {
        if (RequestCloseDrawer != null)
        {
            await RequestCloseDrawer.Invoke();
        }
    }

    [RelayCommand]
    private async Task SelectPeerAsync(DevicePeer? peer)
    {
        if (peer == null) return;
        SelectedTarget = peer;
        await CloseDrawerAsync();
    }

    [RelayCommand]
    private void SetIndonesian()
    {
        Strings.SetLanguage("id");
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(CurrentChatModeBadgeText));
        ApplyFilter();
    }

    [RelayCommand]
    private void SetEnglish()
    {
        Strings.SetLanguage("en");
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(CurrentChatModeBadgeText));
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Strings.ToggleLanguage();
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(CurrentChatModeBadgeText));
        ApplyFilter();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;

        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            await Shell.Current.DisplayAlert(Strings.WarningTitle, Strings.FormatPeerOffline(currentTarget.Name), "OK");
            return;
        }

        var text = MessageInput.Trim();
        MessageInput = string.Empty;

        bool success = await Manager.SendTextMessageAsync(text);
        if (!success && currentTarget != null && !currentTarget.IsBroadcastTarget)
        {
            await Shell.Current.DisplayAlert(Strings.WarningTitle, Strings.FormatSendFailed(currentTarget.Name), "OK");
        }

        ApplyFilter();
    }

    [RelayCommand]
    private async Task PickAndSendPhotoAsync()
    {
        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            await Shell.Current.DisplayAlert(Strings.WarningTitle, Strings.FormatPeerOffline(currentTarget.Name), "OK");
            return;
        }

        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                string? caption = null;
                if (!string.IsNullOrWhiteSpace(MessageInput))
                {
                    caption = MessageInput.Trim();
                    MessageInput = string.Empty;
                }

                await Manager.SendImageMessageAsync(result.FileName, bytes, caption);
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking photo: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PickAndSendVideoAsync()
    {
        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            await Shell.Current.DisplayAlert(Strings.WarningTitle, Strings.FormatPeerOffline(currentTarget.Name), "OK");
            return;
        }

        try
        {
            var result = await MediaPicker.Default.PickVideoAsync();
            if (result != null)
            {
                var fileInfo = new FileInfo(result.FullPath);
                if (fileInfo.Exists && fileInfo.Length > 250 * 1024 * 1024)
                {
                    await Shell.Current.DisplayAlert(Strings.VideoTooLargeTitle, Strings.VideoTooLarge, "OK");
                    return;
                }

                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                string? caption = null;
                if (!string.IsNullOrWhiteSpace(MessageInput))
                {
                    caption = MessageInput.Trim();
                    MessageInput = string.Empty;
                }

                await Manager.SendVideoMessageAsync(result.FileName, bytes, caption);
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking video: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AttachMediaAsync()
    {
        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            await Shell.Current.DisplayAlert(Strings.WarningTitle, Strings.FormatPeerOffline(currentTarget.Name), "OK");
            return;
        }

        string optPhoto = $"📷 {Strings.Photo}";
        string optVideo = $"🎥 {Strings.Video}";

        string action = await Shell.Current.DisplayActionSheet(
            Strings.AttachMediaTitle,
            Strings.Cancel,
            null,
            optPhoto,
            optVideo);

        if (action == optPhoto)
        {
            await PickAndSendPhotoAsync();
        }
        else if (action == optVideo)
        {
            await PickAndSendVideoAsync();
        }
    }

    [RelayCommand]
    private void SaveDeviceName()
    {
        if (!string.IsNullOrWhiteSpace(DeviceName))
        {
            Manager.UpdateLocalDeviceName(DeviceName.Trim());
        }
    }
}
