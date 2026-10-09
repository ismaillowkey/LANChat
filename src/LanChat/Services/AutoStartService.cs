using System;
using System.IO;
using Microsoft.Win32;

namespace LanChat.Services;

public static class AutoStartService
{
    private const string AppName = "LANChat";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsAutoStartEnabled()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            var autostartPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart", "lanchat.desktop");
            return File.Exists(autostartPath);
        }

        return false;
    }

    public static bool SetAutoStart(bool enable)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return false;

                if (enable)
                {
                    var exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        exePath = Path.Combine(AppContext.BaseDirectory, "LanChat.Desktop.exe");
                    }
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config", "autostart");
                var desktopFile = Path.Combine(dir, "lanchat.desktop");

                if (enable)
                {
                    Directory.CreateDirectory(dir);
                    var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "LanChat.Desktop");
                    var content = $"[Desktop Entry]\nType=Application\nName=LAN Chat\nExec=\"{exePath}\"\nHidden=false\nNoDisplay=false\nX-GNOME-Autostart-enabled=true\n";
                    File.WriteAllText(desktopFile, content);
                }
                else
                {
                    if (File.Exists(desktopFile)) File.Delete(desktopFile);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
