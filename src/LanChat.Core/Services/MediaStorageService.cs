using System;
using System.IO;

namespace LanChat.Core.Services;

public static class MediaStorageService
{
    public static string MediaDirectory => StoragePaths.MediaDirectory;

    public static string SaveMedia(string originalFileName, byte[] data)
    {
        try
        {
            var dir = MediaDirectory;
            var safeName = SanitizeFileName(originalFileName);

            var destPath = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}");
            File.WriteAllBytes(destPath, data);
            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving media: {ex.Message}");
            return string.Empty;
        }
    }

    public static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"media_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
        }

        var safe = Path.GetFileName(fileName);
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(c, '_');
        }

        return safe;
    }
}
