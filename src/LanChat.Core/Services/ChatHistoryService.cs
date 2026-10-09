using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LanChat.Core.Models;

namespace LanChat.Core.Services;

public class StoredChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? Content { get; set; }
    public string? FileName { get; set; }
    public string? LocalFilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsOutgoing { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
}

public static class ChatHistoryService
{
    private static readonly object _fileLock = new();

    public static List<ChatMessage> LoadHistory()
    {
        lock (_fileLock)
        {
            var result = new List<ChatMessage>();
            try
            {
                var filePath = StoragePaths.ChatHistoryFilePath;
                if (!File.Exists(filePath)) return result;

                var json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<StoredChatMessage>>(json);
                if (items == null) return result;

                foreach (var item in items)
                {
                    var msg = new ChatMessage
                    {
                        Id = item.Id,
                        SenderId = item.SenderId,
                        SenderName = item.SenderName,
                        TargetId = item.TargetId,
                        TargetName = item.TargetName,
                        Type = item.Type,
                        Content = item.Content,
                        FileName = item.FileName,
                        LocalFilePath = item.LocalFilePath,
                        FileSizeBytes = item.FileSizeBytes,
                        Timestamp = item.Timestamp,
                        IsOutgoing = item.IsOutgoing,
                        Status = item.Status
                    };

                    msg.UpdateMediaState();
                    result.Add(msg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load chat history: {ex.Message}");
            }
            return result;
        }
    }

    public static void AppendMessage(ChatMessage message)
    {
        lock (_fileLock)
        {
            try
            {
                var filePath = StoragePaths.ChatHistoryFilePath;
                List<StoredChatMessage> items;
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    items = JsonSerializer.Deserialize<List<StoredChatMessage>>(json) ?? new List<StoredChatMessage>();
                }
                else
                {
                    items = new List<StoredChatMessage>();
                }

                if (items.Any(i => i.Id == message.Id)) return;

                items.Add(new StoredChatMessage
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    SenderName = message.SenderName,
                    TargetId = message.TargetId,
                    TargetName = message.TargetName,
                    Type = message.Type,
                    Content = message.Content,
                    FileName = message.FileName,
                    LocalFilePath = message.LocalFilePath,
                    FileSizeBytes = message.FileSizeBytes,
                    Timestamp = message.Timestamp,
                    IsOutgoing = message.IsOutgoing,
                    Status = message.Status
                });

                var updatedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to append message to history: {ex.Message}");
            }
        }
    }

    public static void UpdateMessageStatus(string messageId, MessageStatus status)
    {
        lock (_fileLock)
        {
            try
            {
                var filePath = StoragePaths.ChatHistoryFilePath;
                if (!File.Exists(filePath)) return;

                var json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<StoredChatMessage>>(json);
                if (items == null) return;

                var found = items.FirstOrDefault(i => i.Id == messageId);
                if (found != null && found.Status != status)
                {
                    found.Status = status;
                    var updatedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, updatedJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update message status in history: {ex.Message}");
            }
        }
    }

    public static void UpdateMessagesStatus(IEnumerable<string> messageIds, MessageStatus status)
    {
        if (messageIds == null) return;
        var idSet = new HashSet<string>(messageIds);
        if (idSet.Count == 0) return;

        lock (_fileLock)
        {
            try
            {
                var filePath = StoragePaths.ChatHistoryFilePath;
                if (!File.Exists(filePath)) return;

                var json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<StoredChatMessage>>(json);
                if (items == null) return;

                bool modified = false;
                foreach (var item in items)
                {
                    if (idSet.Contains(item.Id) && item.Status != status)
                    {
                        item.Status = status;
                        modified = true;
                    }
                }

                if (modified)
                {
                    var updatedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, updatedJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update messages status in history: {ex.Message}");
            }
        }
    }

    public static void RemoveMessagesForPeer(string peerId)
    {
        lock (_fileLock)
        {
            try
            {
                var filePath = StoragePaths.ChatHistoryFilePath;
                if (!File.Exists(filePath)) return;

                var json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<StoredChatMessage>>(json);
                if (items == null) return;

                items.RemoveAll(m => (m.SenderId == peerId || m.TargetId == peerId) && m.TargetId != DevicePeer.BroadcastTargetId);

                var updatedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove messages for peer: {ex.Message}");
            }
        }
    }
}
