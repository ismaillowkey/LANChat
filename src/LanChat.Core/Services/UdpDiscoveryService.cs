using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LanChat.Core.Models;

namespace LanChat.Core.Services;

public class UdpDiscoveryService : IDisposable
{
    public const int DefaultDiscoveryPort = 45450;
    private readonly int _port;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, (DevicePeer Peer, DateTime LastSeen)> _peers = new();
    private System.Threading.Timer? _cleanupTimer;
    private System.Threading.Timer? _broadcastTimer;

    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    public string LocalDeviceName { get; set; } = Environment.MachineName;
    public string LocalIpAddress { get; set; } = "127.0.0.1";
    public string LocalMacAddress { get; set; } = "00:00:00:00:00:00";
    public int LocalTcpPort { get; set; } = 45451;

    public event Action<DevicePeer>? PeerDiscovered;
    public event Action<DevicePeer>? PeerUpdated;
    public event Action<string>? PeerLost;

    public UdpDiscoveryService(int port = DefaultDiscoveryPort)
    {
        _port = port;
    }

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            _udpClient.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to bind UDP discovery: {ex.Message}");
            return;
        }

        // Listen task
        Task.Run(() => ListenLoopAsync(_cts.Token));

        // Periodic Broadcast announcement
        _broadcastTimer = new System.Threading.Timer(_ =>
        {
            _ = BroadcastAnnouncementAsync("ANNOUNCE");
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(4));

        // Expired Peer cleanup timer
        _cleanupTimer = new System.Threading.Timer(_ =>
        {
            CheckExpiredPeers();
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public async Task BroadcastAnnouncementAsync(string action = "ANNOUNCE")
    {
        if (_udpClient == null) return;

        try
        {
            var beacon = new DiscoveryBeacon
            {
                DeviceId = LocalDeviceId,
                DeviceName = LocalDeviceName,
                SenderIp = LocalIpAddress,
                MacAddress = LocalMacAddress,
                TcpPort = LocalTcpPort,
                Action = action,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var json = JsonSerializer.Serialize(beacon);
            var bytes = Encoding.UTF8.GetBytes(json);

            var broadcastAddresses = NetworkUtils.GetBroadcastAddresses();
            foreach (var addr in broadcastAddresses)
            {
                try
                {
                    await _udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(addr, _port));
                }
                catch
                {
                    // Ignore transient network errors
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending UDP broadcast: {ex.Message}");
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);
                var beacon = JsonSerializer.Deserialize<DiscoveryBeacon>(json);

                if (beacon == null || beacon.DeviceId == LocalDeviceId)
                    continue;

                var senderIp = result.RemoteEndPoint.Address.ToString();
                var localIps = NetworkUtils.GetLocalIPv4Addresses();
                bool isSameMachine = localIps.Any(ip => ip.ToString() == senderIp) || senderIp == "127.0.0.1";
                if (isSameMachine)
                {
                    senderIp = "127.0.0.1";
                }
                else if (!string.IsNullOrEmpty(beacon.SenderIp))
                {
                    senderIp = beacon.SenderIp;
                }

                if (beacon.Action == "BYE")
                {
                    if (_peers.TryRemove(beacon.DeviceId, out _))
                    {
                        PeerLost?.Invoke(beacon.DeviceId);
                    }
                    continue;
                }

                var now = DateTime.UtcNow;
                bool isNew = !_peers.ContainsKey(beacon.DeviceId);

                var peer = new DevicePeer
                {
                    Id = beacon.DeviceId,
                    Name = beacon.DeviceName,
                    IpAddress = senderIp,
                    MacAddress = beacon.MacAddress,
                    TcpPort = beacon.TcpPort,
                    LastSeen = now,
                    IsOnline = true
                };

                _peers[beacon.DeviceId] = (peer, now);

                if (isNew)
                {
                    PeerDiscovered?.Invoke(peer);
                }
                else
                {
                    PeerUpdated?.Invoke(peer);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested) break;
                System.Diagnostics.Debug.WriteLine($"UDP listen error: {ex.Message}");
            }
        }
    }

    private void CheckExpiredPeers()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-12);
        foreach (var kvp in _peers.ToArray())
        {
            if (kvp.Value.LastSeen < cutoff)
            {
                if (_peers.TryRemove(kvp.Key, out var removed))
                {
                    removed.Peer.IsOnline = false;
                    PeerLost?.Invoke(kvp.Key);
                }
            }
        }
    }

    public void Stop()
    {
        try
        {
            _broadcastTimer?.Dispose();
            _broadcastTimer = null;

            _cleanupTimer?.Dispose();
            _cleanupTimer = null;

            // Send bye announcement
            try
            {
                BroadcastAnnouncementAsync("BYE").Wait(500);
            }
            catch { }

            _cts?.Cancel();
            _udpClient?.Dispose();
            _udpClient = null;
            _cts?.Dispose();
            _cts = null;
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
