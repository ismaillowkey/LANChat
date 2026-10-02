using CommunityToolkit.Mvvm.ComponentModel;

namespace LanChat.Core.Models;

public partial class DevicePeer : ObservableObject
{
    public const string BroadcastTargetId = "ALL";

    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private int _tcpPort;

    [ObservableProperty]
    private DateTime _lastSeen = DateTime.UtcNow;

    [ObservableProperty]
    private bool _isOnline = true;

    [ObservableProperty]
    private bool _hasUnread;

    [ObservableProperty]
    private int _unreadCount;

    public bool IsBroadcastTarget => Id == BroadcastTargetId;

    public string DisplayName => IsBroadcastTarget ? "📢 Semua Device (Broadcast)" : $"{Name} ({IpAddress})";

    public static DevicePeer CreateBroadcastTarget()
    {
        return new DevicePeer
        {
            Id = BroadcastTargetId,
            Name = "Semua Device (Broadcast)",
            IpAddress = "255.255.255.255",
            TcpPort = 0,
            IsOnline = true
        };
    }
}
