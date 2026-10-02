using CommunityToolkit.Mvvm.ComponentModel;

namespace LanChat.Core.Models;

public partial class ChatMessage : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SenderId { get; set; } = string.Empty;

    [ObservableProperty]
    private string _senderName = string.Empty;

    public string TargetId { get; set; } = DevicePeer.BroadcastTargetId;

    [ObservableProperty]
    private string _targetName = "Semua Device";

    public MessageType Type { get; set; } = MessageType.Text;

    [ObservableProperty]
    private string? _content;

    [ObservableProperty]
    private string? _fileName;

    [ObservableProperty]
    private byte[]? _imageData;

    [ObservableProperty]
    private string? _localFilePath;

    [ObservableProperty]
    private long _fileSizeBytes;

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [ObservableProperty]
    private bool _isOutgoing;

    public bool IsDirect => TargetId != DevicePeer.BroadcastTargetId;

    public bool IsImage => Type == MessageType.Image;

    public bool IsVideo => Type == MessageType.Video;

    public bool HasMediaFile => !string.IsNullOrEmpty(LocalFilePath) || ImageData != null;

    public string FormattedTime => Timestamp.ToString("HH:mm");

    public string FormattedFileSize
    {
        get
        {
            long bytes = FileSizeBytes;
            if (bytes <= 0 && ImageData != null) bytes = ImageData.Length;
            if (bytes <= 0) return string.Empty;

            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
