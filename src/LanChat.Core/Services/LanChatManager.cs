using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LanChat.Core.Models;

namespace LanChat.Core.Services;

public partial class LanChatManager : ObservableObject, IDisposable
{
    private readonly UdpDiscoveryService _discoveryService;
    private readonly TcpChatTransport _transportService;
    private readonly SynchronizationContext? _syncContext;

    [ObservableProperty]
    private string _localDeviceId = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _localDeviceName = Environment.MachineName;

    [ObservableProperty]
    private string _localIpAddress = "127.0.0.1";

    [ObservableProperty]
    private string _localMacAddress = "00:00:00:00:00:00";

    [ObservableProperty]
    private int _localTcpPort = 45451;

    [ObservableProperty]
    private DevicePeer _selectedTarget;

    public ObservableCollection<DevicePeer> Peers { get; } = new();
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public event Action<ChatMessage>? MessageReceived;

    public LanChatManager(string? customDeviceName = null)
    {
        _syncContext = SynchronizationContext.Current;

        LocalDeviceName = AppSettingsService.LoadDeviceName(customDeviceName ?? Environment.MachineName);
        LocalMacAddress = NetworkUtils.GetLocalMacAddress();

        var localIps = NetworkUtils.GetLocalIPv4Addresses();
        if (localIps.Count > 0)
        {
            LocalIpAddress = localIps[0].ToString();
        }

        _discoveryService = new UdpDiscoveryService();
        _transportService = new TcpChatTransport();

        var broadcastTarget = DevicePeer.CreateBroadcastTarget();
        broadcastTarget.MacAddress = "FF:FF:FF:FF:FF:FF";
        Peers.Add(broadcastTarget);
        _selectedTarget = broadcastTarget;

        _discoveryService.PeerDiscovered += OnPeerDiscovered;
        _discoveryService.PeerUpdated += OnPeerUpdated;
        _discoveryService.PeerLost += OnPeerLost;
        _transportService.MessageReceived += OnIncomingMessage;
    }

    public void Start()
    {
        // 1. Start TCP listener first to obtain the bound port
        LocalTcpPort = _transportService.Start(45451);

        // 2. Configure & Start UDP discovery
        _discoveryService.LocalDeviceId = LocalDeviceId;
        _discoveryService.LocalDeviceName = LocalDeviceName;
        _discoveryService.LocalIpAddress = LocalIpAddress;
        _discoveryService.LocalMacAddress = LocalMacAddress;
        _discoveryService.LocalTcpPort = LocalTcpPort;
        _discoveryService.Start();
    }

    public void UpdateLocalDeviceName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        LocalDeviceName = newName.Trim();
        AppSettingsService.SaveDeviceName(LocalDeviceName);

        _discoveryService.LocalDeviceName = LocalDeviceName;
        _ = _discoveryService.BroadcastAnnouncementAsync("ANNOUNCE");
    }

    private void RunOnUI(Action action)
    {
        if (_syncContext != null)
        {
            _syncContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    private void OnPeerDiscovered(DevicePeer peer)
    {
        RunOnUI(() =>
        {
            var existing = Peers.FirstOrDefault(p => p.Id == peer.Id);
            if (existing == null)
            {
                Peers.Add(peer);
            }
            else
            {
                existing.Name = peer.Name;
                existing.IpAddress = peer.IpAddress;
                existing.MacAddress = peer.MacAddress;
                existing.TcpPort = peer.TcpPort;
                existing.IsOnline = true;
            }
        });
    }

    private void OnPeerUpdated(DevicePeer peer)
    {
        RunOnUI(() =>
        {
            var existing = Peers.FirstOrDefault(p => p.Id == peer.Id);
            if (existing != null)
            {
                existing.Name = peer.Name;
                existing.IpAddress = peer.IpAddress;
                existing.MacAddress = peer.MacAddress;
                existing.TcpPort = peer.TcpPort;
                existing.IsOnline = true;
                existing.LastSeen = peer.LastSeen;
            }
            else
            {
                Peers.Add(peer);
            }
        });
    }

    private void OnPeerLost(string peerId)
    {
        RunOnUI(() =>
        {
            var existing = Peers.FirstOrDefault(p => p.Id == peerId);
            if (existing != null && !existing.IsBroadcastTarget)
            {
                existing.IsOnline = false;
                Peers.Remove(existing);
            }
        });
    }

    private void OnIncomingMessage(ChatMessage msg)
    {
        if (msg.SenderId == LocalDeviceId) return;

        RunOnUI(() =>
        {
            Messages.Add(msg);
            MessageReceived?.Invoke(msg);
        });
    }

    public async Task<bool> SendTextMessageAsync(string text, DevicePeer? target = null)
    {
        target ??= SelectedTarget;
        if (string.IsNullOrWhiteSpace(text) || target == null) return false;

        var msg = new ChatMessage
        {
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Text,
            Content = text,
            IsOutgoing = true,
            Timestamp = DateTime.Now
        };

        var packet = new TransportPacket
        {
            MessageId = msg.Id,
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Text,
            Content = text,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        bool success = await SendPacketToTargetAsync(target, packet);

        if (success)
        {
            RunOnUI(() => Messages.Add(msg));
        }

        return success;
    }

    public async Task<bool> SendImageMessageAsync(string fileName, byte[] imageBytes, string? caption = null, DevicePeer? target = null)
    {
        target ??= SelectedTarget;
        if (imageBytes == null || imageBytes.Length == 0 || target == null) return false;

        var localPath = MediaStorageService.SaveMedia(fileName, imageBytes);

        var msg = new ChatMessage
        {
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Image,
            Content = caption,
            FileName = fileName,
            ImageData = imageBytes,
            LocalFilePath = localPath,
            FileSizeBytes = imageBytes.Length,
            IsOutgoing = true,
            Timestamp = DateTime.Now
        };

        var packet = new TransportPacket
        {
            MessageId = msg.Id,
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Image,
            Content = caption,
            FileName = fileName,
            FileSize = imageBytes.Length,
            MediaBase64 = Convert.ToBase64String(imageBytes),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        bool success = await SendPacketToTargetAsync(target, packet);

        if (success)
        {
            RunOnUI(() => Messages.Add(msg));
        }

        return success;
    }

    public async Task<bool> SendVideoMessageAsync(string fileName, byte[] videoBytes, string? caption = null, DevicePeer? target = null)
    {
        target ??= SelectedTarget;
        if (videoBytes == null || videoBytes.Length == 0 || target == null) return false;

        var localPath = MediaStorageService.SaveMedia(fileName, videoBytes);

        var msg = new ChatMessage
        {
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Video,
            Content = caption,
            FileName = fileName,
            LocalFilePath = localPath,
            FileSizeBytes = videoBytes.Length,
            IsOutgoing = true,
            Timestamp = DateTime.Now
        };

        var packet = new TransportPacket
        {
            MessageId = msg.Id,
            SenderId = LocalDeviceId,
            SenderName = LocalDeviceName,
            TargetId = target.Id,
            TargetName = target.Name,
            Type = MessageType.Video,
            Content = caption,
            FileName = fileName,
            FileSize = videoBytes.Length,
            MediaBase64 = Convert.ToBase64String(videoBytes),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        bool success = await SendPacketToTargetAsync(target, packet);

        if (success)
        {
            RunOnUI(() => Messages.Add(msg));
        }

        return success;
    }

    private async Task<bool> SendPacketToTargetAsync(DevicePeer target, TransportPacket packet)
    {
        if (target.IsBroadcastTarget)
        {
            // Broadcast to all online discovered peers
            var onlinePeers = Peers.Where(p => !p.IsBroadcastTarget && p.IsOnline).ToList();
            if (onlinePeers.Count == 0)
            {
                // Still allow local message to be displayed even if no peers are connected yet
                return true;
            }

            var tasks = onlinePeers.Select(p => _transportService.SendPacketAsync(p.IpAddress, p.TcpPort, packet));
            await Task.WhenAll(tasks);
            return true;
        }
        else
        {
            // Direct message to specific device
            return await _transportService.SendPacketAsync(target.IpAddress, target.TcpPort, packet);
        }
    }

    public void Stop()
    {
        _discoveryService.Stop();
        _transportService.Stop();
    }

    public void Dispose()
    {
        Stop();
        _discoveryService.Dispose();
        _transportService.Dispose();
    }
}
