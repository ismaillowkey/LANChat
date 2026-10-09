using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LanChat.Core.Models;

namespace LanChat.Core.Services;

public class TcpChatTransport : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _boundPort;

    public int BoundPort => _boundPort;
    public event Action<ChatMessage>? MessageReceived;
    public event Action<TransportPacket>? AckReceived;
    public Func<string, bool>? IsChatActiveWithPeer;

    public int Start(int preferredPort = 45451)
    {
        if (_cts != null) return _boundPort;
        _cts = new CancellationTokenSource();

        int portToTry = preferredPort;
        for (int i = 0; i < 50; i++)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, portToTry);
                listener.Start();
                _listener = listener;
                _boundPort = portToTry;
                break;
            }
            catch
            {
                portToTry++;
            }
        }

        if (_listener == null)
        {
            // Fallback to random dynamic port
            _listener = new TcpListener(IPAddress.Any, 0);
            _listener.Start();
            _boundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        Task.Run(() => AcceptClientsLoopAsync(_cts.Token));
        return _boundPort;
    }

    private async Task AcceptClientsLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
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
                System.Diagnostics.Debug.WriteLine($"TCP Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            try
            {
                // Read 4-byte length prefix
                var lengthBuffer = new byte[4];
                int read = await ReadExactAsync(stream, lengthBuffer, 0, 4, ct);
                if (read < 4) return;

                int payloadLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
                if (payloadLength <= 0 || payloadLength > 250 * 1024 * 1024) // 250MB max limit for photos/videos
                    return;

                var payloadBytes = new byte[payloadLength];
                read = await ReadExactAsync(stream, payloadBytes, 0, payloadLength, ct);
                if (read < payloadLength) return;

                var json = Encoding.UTF8.GetString(payloadBytes);
                var packet = JsonSerializer.Deserialize<TransportPacket>(json);
                if (packet == null) return;

                // Handle ACK packets (DELIVERED or READ receipts)
                if (packet.Action == "ACK_DELIVERED" || packet.Action == "ACK_READ")
                {
                    AckReceived?.Invoke(packet);
                    return;
                }

                byte[]? mediaBytes = null;
                string? localFilePath = null;
                long fileSizeBytes = packet.FileSize;

                if (!string.IsNullOrEmpty(packet.MediaBase64))
                {
                    mediaBytes = Convert.FromBase64String(packet.MediaBase64);
                    fileSizeBytes = mediaBytes.Length;
                    localFilePath = MediaStorageService.SaveMedia(packet.FileName ?? "media.bin", mediaBytes);
                }

                var msg = new ChatMessage
                {
                    Id = packet.MessageId,
                    SenderId = packet.SenderId,
                    SenderName = packet.SenderName,
                    TargetId = packet.TargetId,
                    TargetName = packet.TargetName,
                    Type = packet.Type,
                    Content = packet.Content,
                    FileName = packet.FileName,
                    ImageData = packet.Type == MessageType.Image ? mediaBytes : null,
                    LocalFilePath = localFilePath,
                    FileSizeBytes = fileSizeBytes,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(packet.Timestamp).LocalDateTime,
                    IsOutgoing = false,
                    Status = MessageStatus.Delivered
                };

                MessageReceived?.Invoke(msg);

                // If this is a direct message, immediately acknowledge receipt on the same connection
                if (packet.TargetId != DevicePeer.BroadcastTargetId)
                {
                    try
                    {
                        bool isRead = IsChatActiveWithPeer?.Invoke(packet.SenderId) == true;
                        var ackPacket = new TransportPacket
                        {
                            MessageId = packet.MessageId,
                            Action = isRead ? "ACK_READ" : "ACK_DELIVERED",
                            SenderId = packet.TargetId,
                            TargetId = packet.SenderId
                        };
                        var ackJson = JsonSerializer.Serialize(ackPacket);
                        var ackBytes = Encoding.UTF8.GetBytes(ackJson);
                        var ackLenBuf = new byte[4];
                        BinaryPrimitives.WriteInt32BigEndian(ackLenBuf, ackBytes.Length);
                        await stream.WriteAsync(ackLenBuf, 0, 4, ct);
                        await stream.WriteAsync(ackBytes, 0, ackBytes.Length, ct);
                        await stream.FlushAsync(ct);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing incoming packet: {ex.Message}");
            }
        }
    }

    public async Task<bool> SendPacketAsync(string targetIp, int targetPort, TransportPacket packet, int connectTimeoutMs = 6000)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Parse(targetIp), targetPort);
            if (await Task.WhenAny(connectTask, Task.Delay(connectTimeoutMs)) != connectTask)
            {
                System.Diagnostics.Debug.WriteLine($"TCP connect to {targetIp}:{targetPort} timed out ({connectTimeoutMs}ms)");
                return false;
            }
            await connectTask;

            var json = JsonSerializer.Serialize(packet);
            var payloadBytes = Encoding.UTF8.GetBytes(json);

            // Generous timeout for payload transfer (at least 30s for media, or calculated based on size)
            int transferTimeoutMs = packet.Type == MessageType.Text ? 15000 : Math.Max(45000, (payloadBytes.Length / (20 * 1024)) * 1000);
            using var cts = new CancellationTokenSource(transferTimeoutMs);

            using var stream = client.GetStream();

            var lengthBuffer = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, payloadBytes.Length);

            await stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length, cts.Token);
            await stream.WriteAsync(payloadBytes, 0, payloadBytes.Length, cts.Token);
            await stream.FlushAsync(cts.Token);

            // For direct messages, wait for receiver's immediate ACK response on this stream
            if (packet.Action == "MESSAGE" && packet.TargetId != DevicePeer.BroadcastTargetId)
            {
                try
                {
                    using var ackCts = new CancellationTokenSource(4000);
                    var ackLenBuf = new byte[4];
                    int ackRead = await ReadExactAsync(stream, ackLenBuf, 0, 4, ackCts.Token);
                    if (ackRead == 4)
                    {
                        int ackLen = BinaryPrimitives.ReadInt32BigEndian(ackLenBuf);
                        if (ackLen > 0 && ackLen < 65536)
                        {
                            var ackBytes = new byte[ackLen];
                            if (await ReadExactAsync(stream, ackBytes, 0, ackLen, ackCts.Token) == ackLen)
                            {
                                var ackJson = Encoding.UTF8.GetString(ackBytes);
                                var ackPacket = JsonSerializer.Deserialize<TransportPacket>(ackJson);
                                if (ackPacket != null)
                                {
                                    AckReceived?.Invoke(ackPacket);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send TCP packet to {targetIp}:{targetPort}: {ex.Message}");
            return false;
        }
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;
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
