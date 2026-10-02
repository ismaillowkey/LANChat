using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanChat.Core.Models;
using LanChat.Core.Services;

namespace LanChat.Maui.ViewModels;

public partial class MainChatViewModel : ObservableObject
{
    public LanChatManager Manager { get; }

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    public MainChatViewModel()
    {
        Manager = new LanChatManager(DeviceInfo.Current.Name);
        DeviceName = Manager.LocalDeviceName;
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
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput)) return;

        var text = MessageInput.Trim();
        MessageInput = string.Empty;

        await Manager.SendTextMessageAsync(text);
    }

    [RelayCommand]
    private async Task PickAndSendPhotoAsync()
    {
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
        try
        {
            var result = await MediaPicker.Default.PickVideoAsync();
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

                await Manager.SendVideoMessageAsync(result.FileName, bytes, caption);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking video: {ex.Message}");
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
