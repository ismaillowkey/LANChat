using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LanChat.Core.Models;

namespace LanChat.Core.Services;

public partial class LanChatManager : ObservableObject, IDisposable
{
    private readonly UdpDiscoveryService _discoveryService;
    private readonly TcpChatTransport _transportService;
    private readonly SynchronizationContext? _syncContext;
    private CancellationTokenSource? _retryCts;

    [ObservableProperty]
    private string _localDeviceId = DeviceIdentityService.GetOrCreateDeviceId();

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

    partial void OnSelectedTargetChanged(DevicePeer? oldValue, DevicePeer newValue)
    {
        void UpdateSelection()
        {
            foreach (var p in Peers)
            {
                p.IsSelected = (newValue != null && p.Id == newValue.Id);
            }
        }

        if (_syncContext != null && SynchronizationContext.Current != _syncContext)
        {
            _syncContext.Post(_ => UpdateSelection(), null);
        }
        else
        {
            UpdateSelection();
        }
    }

    public ObservableCollection<DevicePeer> Peers { get; } = new();
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public event Action<ChatMessage>? MessageReceived;

    public LanChatManager(string? customDeviceName = null)
    {
        _syncContext = SynchronizationContext.Current;

        LocalDeviceId = DeviceIdentityService.GetOrCreateDeviceId();
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
        broadcastTarget.IsSelected = true;
        Peers.Add(broadcastTarget);
        _selectedTarget = broadcastTarget;

        // Load chat history from local disk
        var history = ChatHistoryService.LoadHistory();
        foreach (var msg in history)
        {
            Messages.Add(msg);
        }
        RestorePeersFromHistory(history);

        _discoveryService.PeerDiscovered += OnPeerDiscovered;
        _discoveryService.PeerUpdated += OnPeerUpdated;
        _discoveryService.PeerLost += OnPeerLost;
        _transportService.MessageReceived += OnIncomingMessage;
        _transportService.IsChatActiveWithPeer = (peerId) =>
        {
            if (!AppLifecycleService.IsForeground) return false;
            if (AppLifecycleService.IsChatWindowVisible?.Invoke() == false) return false;
            return SelectedTarget != null && !SelectedTarget.IsBroadcastTarget && SelectedTarget.Id == peerId;
        };
    }

    private void RestorePeersFromHistory(List<ChatMessage> history)
    {
        var knownDirectPeers = new Dictionary<string, string>(); // Id -> Name

        foreach (var msg in history)
        {
            if (!msg.IsDirect) continue;

            if (msg.IsOutgoing)
            {
                if (!string.IsNullOrEmpty(msg.TargetId) && msg.TargetId != DevicePeer.BroadcastTargetId)
                {
                    knownDirectPeers[msg.TargetId] = string.IsNullOrWhiteSpace(msg.TargetName) ? "Device" : msg.TargetName;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(msg.SenderId) && msg.SenderId != LocalDeviceId)
                {
                    knownDirectPeers[msg.SenderId] = string.IsNullOrWhiteSpace(msg.SenderName) ? "Device" : msg.SenderName;
                }
            }
        }

        foreach (var kvp in knownDirectPeers)
        {
            if (Peers.All(p => p.Id != kvp.Key))
            {
                Peers.Add(new DevicePeer
                {
                    Id = kvp.Key,
                    Name = kvp.Value,
                    IpAddress = "Offline",
                    MacAddress = "-",
                    IsOnline = false,
                    IsSelected = (SelectedTarget != null && kvp.Key == SelectedTarget.Id)
                });
            }
        }
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

        // 3. Start 5-second background retry loop for pending direct messages
        _retryCts?.Cancel();
        _retryCts = new CancellationTokenSource();
        Task.Run(() => RetryPendingMessagesLoopAsync(_retryCts.Token));
    }

    public void UpdateLocalDeviceName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        LocalDeviceName = newName.Trim();
        AppSettingsService.SaveDeviceName(LocalDeviceName);

        _discoveryService.LocalDeviceName = LocalDeviceName;
        _ = _discoveryService.BroadcastAnnouncementAsync("ANNOUNCE");
    }

    public void RemovePeer(DevicePeer peer)
    {
        if (peer == null || peer.IsBroadcastTarget) return;

        RunOnUI(() =>
        {
            Peers.Remove(peer);

            if (SelectedTarget?.Id == peer.Id)
            {
                SelectedTarget = Peers.FirstOrDefault(p => p.IsBroadcastTarget) ?? DevicePeer.CreateBroadcastTarget();
            }

            var toRemove = Messages.Where(m => m.IsDirect && (m.SenderId == peer.Id || m.TargetId == peer.Id)).ToList();
            foreach (var m in toRemove)
            {
                Messages.Remove(m);
            }
        });

        ChatHistoryService.RemoveMessagesForPeer(peer.Id);
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
                peer.IsSelected = (SelectedTarget != null && peer.Id == SelectedTarget.Id);
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
                peer.IsSelected = (SelectedTarget != null && peer.Id == SelectedTarget.Id);
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
            }
        });
    }

    private void OnIncomingMessage(ChatMessage msg)
    {
        if (msg.SenderId == LocalDeviceId) return;

        // Deduplication: if message already exists, do not duplicate
        lock (Messages)
        {
            if (Messages.Any(m => m.Id == msg.Id))
            {
                return;
            }
        }

        msg.UpdateMediaState();
        ChatHistoryService.AppendMessage(msg);

        RunOnUI(() =>
        {
            Messages.Add(msg);
            MessageReceived?.Invoke(msg);
        });
    }

    private void OnAckReceived(TransportPacket ack)
    {
        RunOnUI(() =>
        {
            var targetStatus = ack.Action == "ACK_READ" ? MessageStatus.Read : MessageStatus.Delivered;
            var toUpdate = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(ack.MessageId))
            {
                var msg = Messages.FirstOrDefault(m => m.Id == ack.MessageId);
                if (msg != null && msg.Status < targetStatus)
                {
                    toUpdate.Add(msg);
                }
            }

            if (ack.AckMessageIds != null && ack.AckMessageIds.Count > 0)
            {
                var idSet = new HashSet<string>(ack.AckMessageIds);
                var msgs = Messages.Where(m => idSet.Contains(m.Id) && m.Status < targetStatus);
                toUpdate.AddRange(msgs);
            }

            if (ack.Action == "ACK_READ" && !string.IsNullOrEmpty(ack.SenderId))
            {
                // All outgoing direct messages to this peer are marked Read
                var msgs = Messages.Where(m => m.IsOutgoing && m.IsDirect && m.TargetId == ack.SenderId && m.Status < MessageStatus.Read);
                toUpdate.AddRange(msgs);
            }

            foreach (var m in toUpdate.Distinct())
            {
                m.Status = targetStatus;
                ChatHistoryService.UpdateMessageStatus(m.Id, targetStatus);
            }
        });
    }

    public void MarkChatAsRead(string peerId)
    {
        if (string.IsNullOrEmpty(peerId) || peerId == DevicePeer.BroadcastTargetId) return;

        var peer = Peers.FirstOrDefault(p => p.Id == peerId);
        if (peer != null)
        {
            peer.HasUnread = false;
            peer.UnreadCount = 0;
        }

        var unreadMsgIds = Messages.Where(m => !m.IsOutgoing && m.IsDirect && m.SenderId == peerId).Select(m => m.Id).ToList();

        if (peer != null && peer.IsOnline && peer.IpAddress != "Offline")
        {
            var ackPacket = new TransportPacket
            {
                Action = "ACK_READ",
                SenderId = LocalDeviceId,
                TargetId = peerId,
                AckMessageIds = unreadMsgIds
            };
            _ = _transportService.SendPacketAsync(peer.IpAddress, peer.TcpPort, ackPacket);
        }
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
            Status = MessageStatus.Sent,
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

        // Always save to history and UI immediately (starts at Centang 1 Sent)
        ChatHistoryService.AppendMessage(msg);
        RunOnUI(() => Messages.Add(msg));

        // Attempt immediate transmission
        bool success = await SendPacketToTargetAsync(target, packet);
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
            Status = MessageStatus.Sent,
            Timestamp = DateTime.Now
        };

        msg.UpdateMediaState();

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

        // Always save to history and UI immediately
        ChatHistoryService.AppendMessage(msg);
        RunOnUI(() => Messages.Add(msg));

        bool success = await SendPacketToTargetAsync(target, packet);
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
            Status = MessageStatus.Sent,
            Timestamp = DateTime.Now
        };

        msg.UpdateMediaState();

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

        // Always save to history and UI immediately
        ChatHistoryService.AppendMessage(msg);
        RunOnUI(() => Messages.Add(msg));

        bool success = await SendPacketToTargetAsync(target, packet);
        return success;
    }

    private async Task<bool> SendPacketToTargetAsync(DevicePeer target, TransportPacket packet)
    {
        if (target.IsBroadcastTarget)
        {
            var onlinePeers = Peers.Where(p => !p.IsBroadcastTarget && p.IsOnline && p.IpAddress != "Offline").ToList();
            if (onlinePeers.Count == 0) return true;

            var tasks = onlinePeers.Select(p => _transportService.SendPacketAsync(p.IpAddress, p.TcpPort, packet));
            await Task.WhenAll(tasks);
            return true;
        }
        else
        {
            if (!target.IsOnline || target.IpAddress == "Offline")
            {
                return false;
            }
            return await _transportService.SendPacketAsync(target.IpAddress, target.TcpPort, packet);
        }
    }

    private async Task RetryPendingMessagesLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, ct);

                var now = DateTime.Now;
                List<ChatMessage> pending;
                lock (Messages)
                {
                    pending = Messages.Where(m =>
                        m.IsOutgoing &&
                        m.IsDirect &&
                        m.Status == MessageStatus.Sent &&
                        (now - m.Timestamp).TotalHours <= 48
                    ).ToList();
                }

                foreach (var msg in pending)
                {
                    if (ct.IsCancellationRequested) break;

                    var target = Peers.FirstOrDefault(p => p.Id == msg.TargetId);
                    if (target == null || !target.IsOnline || target.IpAddress == "Offline")
                        continue;

                    var packet = new TransportPacket
                    {
                        MessageId = msg.Id,
                        SenderId = LocalDeviceId,
                        SenderName = LocalDeviceName,
                        TargetId = target.Id,
                        TargetName = target.Name,
                        Type = msg.Type,
                        Content = msg.Content,
                        FileName = msg.FileName,
                        FileSize = msg.FileSizeBytes,
                        Timestamp = new DateTimeOffset(msg.Timestamp).ToUnixTimeMilliseconds()
                    };

                    if (msg.Type == MessageType.Image)
                    {
                        if (msg.ImageData != null)
                        {
                            packet.MediaBase64 = Convert.ToBase64String(msg.ImageData);
                        }
                        else if (!string.IsNullOrEmpty(msg.LocalFilePath) && File.Exists(msg.LocalFilePath))
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(msg.LocalFilePath, ct);
                                packet.MediaBase64 = Convert.ToBase64String(bytes);
                            }
                            catch { }
                        }
                    }
                    else if (msg.Type == MessageType.Video && !string.IsNullOrEmpty(msg.LocalFilePath) && File.Exists(msg.LocalFilePath))
                    {
                        try
                        {
                            var bytes = await File.ReadAllBytesAsync(msg.LocalFilePath, ct);
                            packet.MediaBase64 = Convert.ToBase64String(bytes);
                        }
                        catch { }
                    }

                    await _transportService.SendPacketAsync(target.IpAddress, target.TcpPort, packet);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Retry loop error: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _retryCts?.Cancel();
        _retryCts?.Dispose();
        _retryCts = null;
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
