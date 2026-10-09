using CommunityToolkit.Mvvm.ComponentModel;

namespace LanChat.Core.Models;

public partial class DevicePeer : ObservableObject
{
    public const string BroadcastTargetId = "ALL";

    public string Id { get; set; } = string.Empty;

    private string _name = string.Empty;

    public string Name
    {
        get => IsBroadcastTarget ? Services.LocalizationService.Instance.AllDevicesBroadcast : _name;
        set => SetProperty(ref _name, value);
    }

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private int _tcpPort;

    [ObservableProperty]
    private DateTime _lastSeen = DateTime.UtcNow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _isOnline = true;

    public bool CanDelete => !IsOnline && !IsBroadcastTarget;

    [ObservableProperty]
    private bool _hasUnread;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _unreadCount;

    public bool IsBroadcastTarget => Id == BroadcastTargetId;

    public string DisplayName => IsBroadcastTarget ? Services.LocalizationService.Instance.AllDevicesDisplayName : Name;

    public DevicePeer()
    {
        Services.LocalizationService.Instance.PropertyChanged += (s, e) =>
        {
            if (IsBroadcastTarget)
            {
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DisplayName));
            }
        };
    }

    public static DevicePeer CreateBroadcastTarget()
    {
        return new DevicePeer
        {
            Id = BroadcastTargetId,
            _name = Services.LocalizationService.Instance.AllDevicesBroadcast,
            IpAddress = "255.255.255.255",
            TcpPort = 0,
            IsOnline = true
        };
    }
}
