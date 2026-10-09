using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanChat.Core.Models;
using LanChat.Core.Services;
using LanChat.Services;

namespace LanChat.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public LanChatManager Manager { get; }

    public LocalizationService Strings => LocalizationService.Instance;

    public ObservableCollection<ChatMessage> FilteredMessages { get; } = new();

    public IFilePickerService? FilePickerService { get; set; }

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _editableDeviceName = string.Empty;

    [ObservableProperty]
    private bool _isConfirmDeleteOpen;

    [ObservableProperty]
    private bool _isConfirmChangeDeviceNameOpen;

    [ObservableProperty]
    private string _confirmDeleteMessage = string.Empty;

    private DevicePeer? _peerToDelete;

    [ObservableProperty]
    private bool _isAlertOpen;

    [ObservableProperty]
    private bool _isAttachmentSheetOpen;

    [ObservableProperty]
    private bool _isImageViewerOpen;

    [ObservableProperty]
    private ChatMessage? _viewingMessage;

    [ObservableProperty]
    private string? _viewingImagePath;

    [ObservableProperty]
    private string _alertTitle = string.Empty;

    [ObservableProperty]
    private string _alertMessage = string.Empty;

    [ObservableProperty]
    private bool _isAboutOpen;

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    public Action? RequestExit;
    public Action? RequestShowWindow;
    public Action? RequestScrollToBottom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSidebarVisible))]
    [NotifyPropertyChangedFor(nameof(IsChatAreaVisible))]
    [NotifyPropertyChangedFor(nameof(IsDesktopTopBarVisible))]
    [NotifyPropertyChangedFor(nameof(IsMobileTopBarVisible))]
    [NotifyPropertyChangedFor(nameof(MainContentColumnDefinitions))]
    [NotifyPropertyChangedFor(nameof(ChatAreaColumnIndex))]
    private bool _isMobileMode = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSidebarVisible))]
    [NotifyPropertyChangedFor(nameof(IsChatAreaVisible))]
    [NotifyPropertyChangedFor(nameof(IsMobileTopBarVisible))]
    private bool _isMobileChatOpen;

    public bool IsSidebarVisible => !IsMobileMode || !IsMobileChatOpen;
    public bool IsChatAreaVisible => !IsMobileMode || IsMobileChatOpen;

    public bool IsDesktopTopBarVisible => !IsMobileMode;
    public bool IsMobileTopBarVisible => IsMobileMode && !IsMobileChatOpen;

    public string MainContentColumnDefinitions => IsMobileMode ? "*" : "310, *";
    public int ChatAreaColumnIndex => IsMobileMode ? 0 : 1;

    [RelayCommand]
    public void BackToPeerList()
    {
        IsMobileChatOpen = false;
    }

    public bool HandleBackNavigation()
    {
        if (IsImageViewerOpen)
        {
            IsImageViewerOpen = false;
            ViewingMessage = null;
            ViewingImagePath = null;
            return true;
        }
        if (IsAttachmentSheetOpen)
        {
            IsAttachmentSheetOpen = false;
            return true;
        }
        if (IsConfirmDeleteOpen)
        {
            IsConfirmDeleteOpen = false;
            return true;
        }
        if (IsAlertOpen)
        {
            IsAlertOpen = false;
            return true;
        }
        if (IsConfirmChangeDeviceNameOpen)
        {
            IsConfirmChangeDeviceNameOpen = false;
            return true;
        }
        if (IsMobileChatOpen)
        {
            IsMobileChatOpen = false;
            return true;
        }
        return false;
    }

    public string CurrentChatModeBadgeText => SelectedTarget?.IsBroadcastTarget == true
        ? Strings.ModeBroadcastBadge
        : Strings.ModePrivateBadge;

    public MainViewModel()
    {
        Manager = new LanChatManager();
        EditableDeviceName = Manager.LocalDeviceName;
        IsAutoStartEnabled = AutoStartService.IsAutoStartEnabled();

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
            ApplyFilter();
        };

        Manager.MessageReceived += OnIncomingMessage;

        Strings.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Strings));
            OnPropertyChanged(nameof(IsIndonesian));
            OnPropertyChanged(nameof(IsEnglish));
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

        FirewallHelper.AutoConfigureOnStartup();
        AppLifecycleService.IsChatWindowVisible = () => !IsMobileMode || IsMobileChatOpen;
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
                if (!value.IsBroadcastTarget)
                {
                    Manager.MarkChatAsRead(value.Id);
                }
                else
                {
                    value.HasUnread = false;
                    value.UnreadCount = 0;
                }

                foreach (var item in Manager.Messages)
                {
                    if (item.IsImage || item.IsVideo)
                    {
                        item.UpdateMediaState();
                    }
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentChatModeBadgeText));
                OnPropertyChanged(nameof(TotalUnreadCount));
                OnPropertyChanged(nameof(HasAnyUnread));
                ApplyFilter();
            }
        }
    }

    private void ApplyFilter()
    {
        var currentTarget = Manager.SelectedTarget;
        var filtered = Manager.Messages.Where(msg =>
        {
            if (currentTarget == null || currentTarget.IsBroadcastTarget)
            {
                return !msg.IsDirect;
            }
            return msg.IsDirect && (msg.SenderId == currentTarget.Id || msg.TargetId == currentTarget.Id);
        }).ToList();

        FilteredMessages.Clear();
        foreach (var m in filtered)
        {
            FilteredMessages.Add(m);
        }

        RequestScrollToBottom?.Invoke();
    }

    private void OnIncomingMessage(ChatMessage msg)
    {
        bool isCurrentlyActiveInThisChat = AppLifecycleService.IsForeground
            && (AppLifecycleService.IsChatWindowVisible?.Invoke() ?? true)
            && SelectedTarget != null
            && ((!msg.IsDirect && SelectedTarget.IsBroadcastTarget) || (msg.IsDirect && SelectedTarget.Id == msg.SenderId));

        if (!isCurrentlyActiveInThisChat)
        {
            string contentText = (msg.IsImage ? "📷 Foto" : (msg.IsVideo ? "🎬 Video" : msg.Content)) ?? string.Empty;
            string titleText = (msg.IsDirect ? msg.SenderName : $"{msg.SenderName} (Broadcast)") ?? "LAN Chat";
            NotificationManagerService.Show(titleText, contentText, msg.SenderId);
        }

        if (msg.IsDirect)
        {
            var senderPeer = Manager.Peers.FirstOrDefault(p => p.Id == msg.SenderId);
            if (senderPeer != null)
            {
                if (!isCurrentlyActiveInThisChat)
                {
                    senderPeer.HasUnread = true;
                    senderPeer.UnreadCount++;
                }
                else
                {
                    Manager.MarkChatAsRead(senderPeer.Id);
                }
            }
        }
        else
        {
            var broadcastPeer = Manager.Peers.FirstOrDefault(p => p.IsBroadcastTarget);
            if (broadcastPeer != null && !isCurrentlyActiveInThisChat)
            {
                broadcastPeer.HasUnread = true;
                broadcastPeer.UnreadCount++;
            }
        }

        OnPropertyChanged(nameof(TotalUnreadCount));
        OnPropertyChanged(nameof(HasAnyUnread));

        ApplyFilter();
    }

    public int TotalUnreadCount => Manager.Peers.Where(p => !p.IsBroadcastTarget).Sum(p => p.UnreadCount);
    public bool HasAnyUnread => TotalUnreadCount > 0;

    [RelayCommand]
    public void SelectTarget(DevicePeer? peer)
    {
        if (peer != null)
        {
            if (SelectedTarget == peer)
            {
                RequestScrollToBottom?.Invoke();
            }
            else
            {
                SelectedTarget = peer;
            }

            if (IsMobileMode)
            {
                IsMobileChatOpen = true;
            }
        }
    }

    public bool IsIndonesian => Strings.IsIndonesian;
    public bool IsEnglish => Strings.IsEnglish;

    [RelayCommand]
    private void SetIndonesian()
    {
        Strings.SetLanguage("id");
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(IsIndonesian));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(CurrentChatModeBadgeText));
        ApplyFilter();
    }

    [RelayCommand]
    private void SetEnglish()
    {
        Strings.SetLanguage("en");
        OnPropertyChanged(nameof(Strings));
        OnPropertyChanged(nameof(IsIndonesian));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(CurrentChatModeBadgeText));
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        if (Strings.CurrentLanguage == "id")
        {
            SetEnglish();
        }
        else
        {
            SetIndonesian();
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;

        var text = MessageInput.Trim();
        MessageInput = string.Empty;

        await Manager.SendTextMessageAsync(text);
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleAttachmentSheet()
    {
        IsAttachmentSheetOpen = !IsAttachmentSheetOpen;
    }

    [RelayCommand]
    private void CloseAttachmentSheet()
    {
        IsAttachmentSheetOpen = false;
    }

    [RelayCommand]
    private async Task AttachImageAsync()
    {
        IsAttachmentSheetOpen = false;
        if (FilePickerService == null) return;
        var filePath = await FilePickerService.PickImageFileAsync();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        try
        {
            var bytes = await Task.Run(() => File.ReadAllBytes(filePath));
            var fileName = Path.GetFileName(filePath);

            string? caption = null;
            if (!string.IsNullOrWhiteSpace(MessageInput))
            {
                caption = MessageInput.Trim();
                MessageInput = string.Empty;
            }

            await Manager.SendImageMessageAsync(fileName, bytes, caption);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ShowAlert(Strings.ErrorTitle, ex.Message);
        }
    }

    [RelayCommand]
    private async Task AttachVideoAsync()
    {
        IsAttachmentSheetOpen = false;
        if (FilePickerService == null) return;
        var filePath = await FilePickerService.PickVideoFileAsync();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 250 * 1024 * 1024)
            {
                ShowAlert(Strings.VideoTooLargeTitle, Strings.VideoTooLarge);
                return;
            }

            var bytes = await Task.Run(() => File.ReadAllBytes(filePath));
            var fileName = Path.GetFileName(filePath);

            string? caption = null;
            if (!string.IsNullOrWhiteSpace(MessageInput))
            {
                caption = MessageInput.Trim();
                MessageInput = string.Empty;
            }

            await Manager.SendVideoMessageAsync(fileName, bytes, caption);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ShowAlert(Strings.ErrorTitle, ex.Message);
        }
    }

    [RelayCommand]
    public void OpenImageViewer(ChatMessage? msg)
    {
        if (msg == null || !msg.IsImage) return;

        ViewingMessage = msg;
        ViewingImagePath = msg.LocalFilePath;
        IsImageViewerOpen = true;
    }

    [RelayCommand]
    public void CloseImageViewer()
    {
        IsImageViewerOpen = false;
        ViewingMessage = null;
        ViewingImagePath = null;
    }

    [RelayCommand]
    public void DownloadViewingImage()
    {
        if (string.IsNullOrEmpty(ViewingImagePath) || !File.Exists(ViewingImagePath)) return;

        try
        {
            string fileName = Path.GetFileName(ViewingImagePath);
            var savedPath = PlatformLauncherService.SaveFile(ViewingImagePath, fileName);
            if (!string.IsNullOrEmpty(savedPath))
            {
                ShowAlert("Foto Disimpan", $"Foto berhasil disimpan ke:\n{savedPath}");
            }
        }
        catch (Exception ex)
        {
            ShowAlert("Gagal Menyimpan", ex.Message);
        }
    }

    [RelayCommand]
    public void ShareViewingImage()
    {
        if (string.IsNullOrEmpty(ViewingImagePath) || !File.Exists(ViewingImagePath)) return;

        try
        {
            PlatformLauncherService.ShareFile(ViewingImagePath, "image/*");
        }
        catch (Exception ex)
        {
            ShowAlert("Gagal Membagikan", ex.Message);
        }
    }

    [RelayCommand]
    private void AllowFirewall()
    {
        bool ok = FirewallHelper.RequestElevatedFirewallAccess();
        if (ok)
        {
            ShowAlert(Strings.AllowFirewall, Strings.AllowFirewallSuccess);
        }
        else
        {
            ShowAlert(Strings.AllowFirewall, Strings.AllowFirewallError);
        }
    }

    [RelayCommand]
    private void SaveDeviceName()
    {
        if (string.IsNullOrWhiteSpace(EditableDeviceName))
        {
            ShowAlert(Strings.WarningTitle, Strings.DeviceNameCannotBeEmpty);
            return;
        }

        // Buka dialog konfirmasi sebelum mengubah nama perangkat
        IsConfirmChangeDeviceNameOpen = true;
    }

    [RelayCommand]
    private void ConfirmChangeDeviceNameYes()
    {
        if (!string.IsNullOrWhiteSpace(EditableDeviceName))
        {
            var newName = EditableDeviceName.Trim();
            Manager.UpdateLocalDeviceName(newName);
        }
        IsConfirmChangeDeviceNameOpen = false;
    }

    [RelayCommand]
    private void ConfirmChangeDeviceNameNo()
    {
        EditableDeviceName = Manager.LocalDeviceName;
        IsConfirmChangeDeviceNameOpen = false;
    }

    [RelayCommand]
    private void OpenAppMediaFolder()
    {
        IsAttachmentSheetOpen = false;
        try
        {
            PlatformLauncherService.OpenFolder(StoragePaths.MediaDirectory);
        }
        catch (Exception ex)
        {
            ShowAlert(Strings.ErrorTitle, ex.Message);
        }
    }

    [RelayCommand]
    private void PlayVideo(ChatMessage? message)
    {
        if (message == null || string.IsNullOrEmpty(message.LocalFilePath)) return;

        if (!File.Exists(message.LocalFilePath))
        {
            ShowAlert(Strings.MediaNotFoundTitle, Strings.MediaNotFound);
            return;
        }

        try
        {
            PlatformLauncherService.OpenFile(message.LocalFilePath);
        }
        catch (Exception ex)
        {
            ShowAlert(Strings.ErrorTitle, ex.Message);
        }
    }

    [RelayCommand]
    private void DeletePeer(DevicePeer? peer)
    {
        if (peer == null || peer.IsBroadcastTarget || peer.IsOnline) return;

        _peerToDelete = peer;
        ConfirmDeleteMessage = Strings.FormatConfirmDeletePeer(peer.Name);
        IsConfirmDeleteOpen = true;
    }

    [RelayCommand]
    private void ConfirmDeleteYes()
    {
        if (_peerToDelete != null)
        {
            Manager.RemovePeer(_peerToDelete);
            _peerToDelete = null;
            ApplyFilter();
        }
        IsConfirmDeleteOpen = false;
    }

    [RelayCommand]
    private void ConfirmDeleteNo()
    {
        _peerToDelete = null;
        IsConfirmDeleteOpen = false;
    }

    private void ShowAlert(string title, string message)
    {
        AlertTitle = title;
        AlertMessage = message;
        IsAlertOpen = true;
    }

    [RelayCommand]
    private void CloseAlert()
    {
        IsAlertOpen = false;
    }

    [RelayCommand]
    private void ToggleAutoStart()
    {
        IsAutoStartEnabled = !IsAutoStartEnabled;
        AutoStartService.SetAutoStart(IsAutoStartEnabled);
    }

    [RelayCommand]
    private void ShowAbout()
    {
        IsAboutOpen = true;
    }

    [RelayCommand]
    private void CloseAbout()
    {
        IsAboutOpen = false;
    }

    [RelayCommand]
    private void ExitApp()
    {
        if (RequestExit != null)
        {
            RequestExit.Invoke();
        }
        else
        {
            Environment.Exit(0);
        }
    }
}
