using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LanChat.Core.Services;

public class AppSettingsData
{
    public string DeviceName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public static class AppSettingsService
{
    public static string LoadDeviceName(string defaultName)
    {
        try
        {
            var path = StoragePaths.SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.DeviceName))
                {
                    return data.DeviceName;
                }
            }
        }
        catch { }
        return defaultName;
    }

    public static void SaveDeviceName(string deviceName)
    {
        try
        {
            var path = StoragePaths.SettingsFilePath;
            AppSettingsData data = new();
            if (File.Exists(path))
            {
                var existingJson = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<AppSettingsData>(existingJson) ?? new AppSettingsData();
            }
            data.DeviceName = deviceName;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public static string LoadLanguage()
    {
        try
        {
            // 1. Check user settings file
            var path = StoragePaths.SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.Language))
                {
                    var lang = data.Language.Trim().ToLowerInvariant();
                    if (lang == "id" || lang == "en") return lang;
                }
            }

            // 2. Check install_config.json written by NSIS Installer in application directory
            var installConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install_config.json");
            if (File.Exists(installConfigPath))
            {
                var json = File.ReadAllText(installConfigPath);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.Language))
                {
                    var lang = data.Language.Trim().ToLowerInvariant();
                    if (lang == "id" || lang == "en")
                    {
                        SaveLanguage(lang);
                        return lang;
                    }
                }
            }

            // 3. Fallback: OS culture
            var osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (osLang == "en") return "en";
        }
        catch { }
        return "id";
    }

    public static void SaveLanguage(string lang)
    {
        try
        {
            var path = StoragePaths.SettingsFilePath;
            AppSettingsData data = new();
            if (File.Exists(path))
            {
                var existingJson = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<AppSettingsData>(existingJson) ?? new AppSettingsData();
            }
            data.Language = lang.ToLowerInvariant();
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
