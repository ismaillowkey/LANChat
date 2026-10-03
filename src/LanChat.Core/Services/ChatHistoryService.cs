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
                        IsOutgoing = item.IsOutgoing
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
                    IsOutgoing = message.IsOutgoing
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
}
