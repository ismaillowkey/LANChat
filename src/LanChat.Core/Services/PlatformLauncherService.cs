using System;
using System.Diagnostics;
using System.IO;

namespace LanChat.Core.Services;

public static class PlatformLauncherService
{
    public static Action<string>? CustomFileOpener { get; set; }
    public static Action? CustomFolderOpener { get; set; }
    public static Action<string, string?>? CustomFileSharer { get; set; }
    public static Func<string, string, string?>? CustomFileSaver { get; set; }

    public static void ShareFile(string filePath, string? mimeType = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        if (CustomFileSharer != null)
        {
            CustomFileSharer(filePath, mimeType);
            return;
        }

        OpenFile(filePath);
    }

    public static string? SaveFile(string sourceFilePath, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath)) return null;

        if (CustomFileSaver != null)
        {
            return CustomFileSaver(sourceFilePath, defaultFileName);
        }

        try
        {
            string downloadsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "LanChat");
            if (!Directory.Exists(downloadsDir)) Directory.CreateDirectory(downloadsDir);

            string destPath = Path.Combine(downloadsDir, defaultFileName);
            File.Copy(sourceFilePath, destPath, true);
            return destPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save file: {ex.Message}");
            return null;
        }
    }

    public static void OpenFolder(string folderPath)
    {
        if (CustomFolderOpener != null)
        {
            CustomFolderOpener();
            return;
        }

        if (string.IsNullOrWhiteSpace(folderPath)) return;

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
        }
    }

    public static void OpenFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        if (CustomFileOpener != null)
        {
            CustomFileOpener(filePath);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true
            });
        }
    }
}
