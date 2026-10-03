using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanChat.Core.Models;
using LanChat.Core.Services;
using Microsoft.Win32;

namespace LanChat.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public LanChatManager Manager { get; }

    public LocalizationService Strings => LocalizationService.Instance;

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _editableDeviceName = string.Empty;

    private readonly ICollectionView _filteredMessagesView;

    public ICollectionView MessagesView => _filteredMessagesView;

    public MainWindowViewModel()
    {
        Manager = new LanChatManager();
        EditableDeviceName = Manager.LocalDeviceName;

        _filteredMessagesView = CollectionViewSource.GetDefaultView(Manager.Messages);
        _filteredMessagesView.Filter = FilterMessage;

        Manager.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Manager.SelectedTarget))
            {
                OnPropertyChanged(nameof(SelectedTarget));
                _filteredMessagesView.Refresh();
            }
        };

        Manager.Messages.CollectionChanged += (s, e) =>
        {
            _filteredMessagesView.Refresh();
        };

        Manager.MessageReceived += OnIncomingMessage;

        Strings.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Strings));
            _filteredMessagesView.Refresh();
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
                // Clear unread bell when clicking into this chat
                value.HasUnread = false;
                value.UnreadCount = 0;

                // Refresh media states for current items in view
                foreach (var item in Manager.Messages)
                {
                    if (item.IsImage || item.IsVideo)
                    {
                        item.UpdateMediaState();
                    }
                }

                OnPropertyChanged();
                _filteredMessagesView.Refresh();
            }
        }
    }

    private void OnIncomingMessage(ChatMessage msg)
    {
        if (msg.IsDirect)
        {
            // Direct message received
            var senderPeer = Manager.Peers.FirstOrDefault(p => p.Id == msg.SenderId);
            if (senderPeer != null)
            {
                // If not currently selected, trigger bell notification!
                if (SelectedTarget == null || SelectedTarget.Id != senderPeer.Id)
                {
                    senderPeer.HasUnread = true;
                    senderPeer.UnreadCount++;
                }
            }
        }
        else
        {
            // Broadcast message received
            var broadcastPeer = Manager.Peers.FirstOrDefault(p => p.IsBroadcastTarget);
            if (broadcastPeer != null && (SelectedTarget == null || !SelectedTarget.IsBroadcastTarget))
            {
                broadcastPeer.HasUnread = true;
                broadcastPeer.UnreadCount++;
            }
        }

        _filteredMessagesView.Refresh();
    }

    private bool FilterMessage(object item)
    {
        if (item is not ChatMessage msg) return false;

        var currentTarget = Manager.SelectedTarget;
        if (currentTarget == null || currentTarget.IsBroadcastTarget)
        {
            // ONLY show Broadcast messages in the Broadcast room
            return !msg.IsDirect;
        }

        // ONLY show Direct messages between me and the selected device
        return msg.IsDirect && (msg.SenderId == currentTarget.Id || msg.TargetId == currentTarget.Id);
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Strings.ToggleLanguage();
        OnPropertyChanged(nameof(Strings));
        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private void SetIndonesian()
    {
        Strings.SetLanguage("id");
        OnPropertyChanged(nameof(Strings));
        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private void SetEnglish()
    {
        Strings.SetLanguage("en");
        OnPropertyChanged(nameof(Strings));
        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;

        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            System.Windows.MessageBox.Show(Strings.FormatPeerOffline(currentTarget.Name), Strings.WarningTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var text = MessageInput.Trim();
        MessageInput = string.Empty;

        bool success = await Manager.SendTextMessageAsync(text);
        if (!success && currentTarget != null && !currentTarget.IsBroadcastTarget)
        {
            System.Windows.MessageBox.Show(Strings.FormatSendFailed(currentTarget.Name), Strings.WarningTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private async Task AttachImageAsync()
    {
        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            System.Windows.MessageBox.Show(Strings.FormatPeerOffline(currentTarget.Name), Strings.WarningTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp|All Files (*.*)|*.*",
            Title = Strings.PickImageTitle
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var bytes = await Task.Run(() => File.ReadAllBytes(dialog.FileName));
                var fileName = Path.GetFileName(dialog.FileName);

                string? caption = null;
                if (!string.IsNullOrWhiteSpace(MessageInput))
                {
                    caption = MessageInput.Trim();
                    MessageInput = string.Empty;
                }

                await Manager.SendImageMessageAsync(fileName, bytes, caption);
                _filteredMessagesView.Refresh();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorTitle + ": {0}", ex.Message), Strings.ErrorTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task AttachVideoAsync()
    {
        var currentTarget = SelectedTarget;
        if (currentTarget != null && !currentTarget.IsBroadcastTarget && !currentTarget.IsOnline)
        {
            System.Windows.MessageBox.Show(Strings.FormatPeerOffline(currentTarget.Name), Strings.WarningTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Video Files (*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.m4v)|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.m4v|All Files (*.*)|*.*",
            Title = Strings.PickVideoTitle
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var fileInfo = new FileInfo(dialog.FileName);
                if (fileInfo.Length > 250 * 1024 * 1024)
                {
                    System.Windows.MessageBox.Show(Strings.VideoTooLarge, Strings.VideoTooLargeTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var bytes = await Task.Run(() => File.ReadAllBytes(dialog.FileName));
                var fileName = Path.GetFileName(dialog.FileName);

                string? caption = null;
                if (!string.IsNullOrWhiteSpace(MessageInput))
                {
                    caption = MessageInput.Trim();
                    MessageInput = string.Empty;
                }

                await Manager.SendVideoMessageAsync(fileName, bytes, caption);
                _filteredMessagesView.Refresh();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorTitle + ": {0}", ex.Message), Strings.ErrorTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void PlayMedia(ChatMessage? msg)
    {
        if (msg == null) return;
        msg.UpdateMediaState();

        if (msg.IsMediaDeleted || string.IsNullOrEmpty(msg.LocalFilePath) || !File.Exists(msg.LocalFilePath))
        {
            System.Windows.MessageBox.Show(Strings.MediaNotFound, Strings.MediaNotFoundTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            _filteredMessagesView.Refresh();
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(msg.LocalFilePath)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(string.Format(Strings.ErrorTitle + ": {0}", ex.Message), Strings.ErrorTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenMediaFolder(ChatMessage? msg)
    {
        try
        {
            if (msg != null && !string.IsNullOrEmpty(msg.LocalFilePath) && File.Exists(msg.LocalFilePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{msg.LocalFilePath}\"");
            }
            else if (Directory.Exists(StoragePaths.MediaDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", StoragePaths.MediaDirectory);
            }
        }
        catch { }
    }

    [RelayCommand]
    private void OpenAppMediaFolder()
    {
        try
        {
            if (!Directory.Exists(StoragePaths.MediaDirectory))
            {
                Directory.CreateDirectory(StoragePaths.MediaDirectory);
            }
            System.Diagnostics.Process.Start("explorer.exe", StoragePaths.MediaDirectory);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(string.Format(Strings.ErrorTitle + ": {0}", ex.Message), Strings.ErrorTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RefreshMediaState()
    {
        foreach (var msg in Manager.Messages)
        {
            if (msg.IsImage || msg.IsVideo)
            {
                msg.UpdateMediaState();
            }
        }
        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private void SaveDeviceName()
    {
        if (!string.IsNullOrWhiteSpace(EditableDeviceName))
        {
            Manager.UpdateLocalDeviceName(EditableDeviceName.Trim());
        }
    }
}
