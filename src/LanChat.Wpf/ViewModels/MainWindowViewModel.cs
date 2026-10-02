using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;

        var text = MessageInput.Trim();
        MessageInput = string.Empty;

        await Manager.SendTextMessageAsync(text);
        _filteredMessagesView.Refresh();
    }

    [RelayCommand]
    private async Task AttachImageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp|All Files (*.*)|*.*",
            Title = "Pilih Gambar untuk Dikirim"
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
                System.Windows.MessageBox.Show($"Gagal mengirim gambar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task AttachVideoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files (*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.m4v)|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.m4v|All Files (*.*)|*.*",
            Title = "Pilih Video untuk Dikirim"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var fileInfo = new FileInfo(dialog.FileName);
                if (fileInfo.Length > 250 * 1024 * 1024)
                {
                    System.Windows.MessageBox.Show("Ukuran video melebihi batas 250 MB.", "File Terlalu Besar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
                System.Windows.MessageBox.Show($"Gagal mengirim video: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void PlayMedia(ChatMessage? msg)
    {
        if (msg == null) return;
        try
        {
            if (!string.IsNullOrEmpty(msg.LocalFilePath) && File.Exists(msg.LocalFilePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(msg.LocalFilePath)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show("File media tidak ditemukan di penyimpanan lokal.", "Perhatian", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Gagal membuka file media: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenMediaFolder(ChatMessage? msg)
    {
        if (msg == null) return;
        try
        {
            if (!string.IsNullOrEmpty(msg.LocalFilePath) && File.Exists(msg.LocalFilePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{msg.LocalFilePath}\"");
            }
            else if (Directory.Exists(MediaStorageService.MediaDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", MediaStorageService.MediaDirectory);
            }
        }
        catch { }
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
