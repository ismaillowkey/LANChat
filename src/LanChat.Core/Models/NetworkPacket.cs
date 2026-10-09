namespace LanChat.Core.Models;

public class DiscoveryBeacon
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string SenderIp { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public int TcpPort { get; set; }
    public string Action { get; set; } = "ANNOUNCE"; // "ANNOUNCE" or "BYE"
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class TransportPacket
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string TargetId { get; set; } = DevicePeer.BroadcastTargetId;
    public string TargetName { get; set; } = "Semua Device";
    public MessageType Type { get; set; } = MessageType.Text;
    public string Action { get; set; } = "MESSAGE"; // "MESSAGE", "ACK_DELIVERED", "ACK_READ"
    public List<string>? AckMessageIds { get; set; }
    public int SenderPort { get; set; }
    public string? Content { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? MediaBase64 { get; set; }
    public string? ImageBase64
    {
        get => MediaBase64;
        set => MediaBase64 = value;
    }
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
