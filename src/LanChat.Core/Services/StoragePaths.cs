using System;
using System.IO;

namespace LanChat.Core.Services;

public static class StoragePaths
{
    public static string AppDataDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            var dir = Path.Combine(appData, "LanChat");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }

    public static string MediaDirectory
    {
        get
        {
            var dir = Path.Combine(AppDataDirectory, "Media");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }

    public static string SettingsFilePath => Path.Combine(AppDataDirectory, "settings.json");

    public static string ChatHistoryFilePath => Path.Combine(AppDataDirectory, "chat_history.json");
}
