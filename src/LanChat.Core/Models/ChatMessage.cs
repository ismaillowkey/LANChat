using System;
using System.IO;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStatusCheckmark))]
    [NotifyPropertyChangedFor(nameof(StatusCheckmarkIcon))]
    [NotifyPropertyChangedFor(nameof(StatusCheckmarkColor))]
    [NotifyPropertyChangedFor(nameof(StatusTooltip))]
    private MessageStatus _status = MessageStatus.Sent;

    public bool ShowStatusCheckmark => IsOutgoing && IsDirect;

    public string StatusCheckmarkIcon => Status switch
    {
        MessageStatus.Sent => "✓",
        MessageStatus.Delivered => "✓✓",
        MessageStatus.Read => "✓✓",
        _ => "✓"
    };

    public string StatusCheckmarkColor => Status switch
    {
        MessageStatus.Read => "#38BDF8", // WhatsApp Sky Blue
        _ => "#CBD5E1"                   // Light Grey on Indigo
    };

    public string StatusTooltip => Status switch
    {
        MessageStatus.Sent => Services.LocalizationService.Instance.IsEnglish 
            ? "Sent (waiting for receiver)" 
            : "Terkirim (belum diterima receiver)",
        MessageStatus.Delivered => Services.LocalizationService.Instance.IsEnglish 
            ? "Delivered (received by device)" 
            : "Terkirim & diterima (belum dibaca)",
        MessageStatus.Read => Services.LocalizationService.Instance.IsEnglish 
            ? "Read by receiver" 
            : "Sudah dibaca oleh receiver",
        _ => string.Empty
    };

    [ObservableProperty]
    private bool _isMediaDeleted;

    [ObservableProperty]
    private bool _hasImagePreview;

    public bool IsDirect => TargetId != DevicePeer.BroadcastTargetId;

    public bool IsImage => Type == MessageType.Image;

    public bool IsVideo => Type == MessageType.Video;

    public bool IsMissingImage => IsImage && (IsMediaDeleted || !HasImagePreview);

    public bool IsMissingVideo => IsVideo && IsMediaDeleted;

    public bool HasMediaFile => !string.IsNullOrEmpty(LocalFilePath) || ImageData != null;

    public string FormattedTime => Timestamp.ToString("HH:mm");

    public string MissingMediaNotice
    {
        get
        {
            var isEn = Services.LocalizationService.Instance.IsEnglish;
            var sender = IsOutgoing 
                ? (isEn ? "You" : "Anda") 
                : (string.IsNullOrWhiteSpace(SenderName) ? (isEn ? "User" : "Pengguna") : SenderName);

            if (Type == MessageType.Image)
            {
                return isEn ? $"{sender} sent an image" : $"{sender} mengirim gambar";
            }
            else if (Type == MessageType.Video)
            {
                return isEn ? $"{sender} sent a video" : $"{sender} mengirim video";
            }
            return isEn ? $"{sender} sent a file" : $"{sender} mengirim file";
        }
    }

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

    public void UpdateMediaState()
    {
        if (Type == MessageType.Image)
        {
            bool fileExists = !string.IsNullOrEmpty(LocalFilePath) && File.Exists(LocalFilePath);
            if (fileExists)
            {
                IsMediaDeleted = false;
                if (ImageData == null)
                {
                    try
                    {
                        ImageData = File.ReadAllBytes(LocalFilePath!);
                    }
                    catch
                    {
                        IsMediaDeleted = true;
                        ImageData = null;
                    }
                }
                HasImagePreview = ImageData != null;
            }
            else
            {
                IsMediaDeleted = true;
                HasImagePreview = false;
                ImageData = null;
            }
        }
        else if (Type == MessageType.Video)
        {
            bool fileExists = !string.IsNullOrEmpty(LocalFilePath) && File.Exists(LocalFilePath);
            IsMediaDeleted = !fileExists;
        }
        else
        {
            IsMediaDeleted = false;
            HasImagePreview = false;
        }

        OnPropertyChanged(nameof(IsMissingImage));
        OnPropertyChanged(nameof(IsMissingVideo));
        OnPropertyChanged(nameof(MissingMediaNotice));
    }
}
