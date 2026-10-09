using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LanChat.Core.Services;

public class AppSettingsData
{
    public string DeviceName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public static class AppSettingsService
{
    public static string LoadDeviceId()
    {
        try
        {
            // 1. Check %APPDATA%\LanChat\config.ini
            var iniVal = IniConfigFile.ReadValue(StoragePaths.IniConfigFilePath, "General", "DeviceId");
            if (!string.IsNullOrWhiteSpace(iniVal)) return iniVal!.Trim();

            // 2. Check AppDir config.ini
            var appDirIniVal = IniConfigFile.ReadValue(StoragePaths.AppDirIniConfigFilePath, "General", "DeviceId");
            if (!string.IsNullOrWhiteSpace(appDirIniVal)) return appDirIniVal!.Trim();

            // 3. Fallback to settings.json
            var path = StoragePaths.SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.DeviceId))
                {
                    return data.DeviceId.Trim();
                }
            }
        }
        catch { }
        return string.Empty;
    }

    public static void SaveDeviceId(string deviceId)
    {
        try
        {
            var cleanId = deviceId.Trim();

            // 1. Save to %APPDATA%\LanChat\config.ini
            IniConfigFile.WriteValue(StoragePaths.IniConfigFilePath, "General", "DeviceId", cleanId);

            // 2. Also save to AppDir config.ini if exists/writable
            try
            {
                if (File.Exists(StoragePaths.AppDirIniConfigFilePath))
                {
                    IniConfigFile.WriteValue(StoragePaths.AppDirIniConfigFilePath, "General", "DeviceId", cleanId);
                }
            }
            catch { }

            // 3. Save to settings.json
            var path = StoragePaths.SettingsFilePath;
            AppSettingsData data = new();
            if (File.Exists(path))
            {
                var existingJson = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<AppSettingsData>(existingJson) ?? new AppSettingsData();
            }
            data.DeviceId = cleanId;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
    public static Func<string>? DefaultDeviceNameResolver { get; set; }

    public static string LoadDeviceName(string defaultName)
    {
        if (DefaultDeviceNameResolver != null)
        {
            var resolved = DefaultDeviceNameResolver();
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                defaultName = resolved.Trim();
            }
        }

        try
        {
            // 1. Check %APPDATA%\LanChat\config.ini
            var iniVal = IniConfigFile.ReadValue(StoragePaths.IniConfigFilePath, "General", "DeviceName");
            if (!string.IsNullOrWhiteSpace(iniVal) && !iniVal.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return iniVal!.Trim();

            // 2. Check AppDir config.ini
            var appDirIniVal = IniConfigFile.ReadValue(StoragePaths.AppDirIniConfigFilePath, "General", "DeviceName");
            if (!string.IsNullOrWhiteSpace(appDirIniVal) && !appDirIniVal.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return appDirIniVal!.Trim();

            // 3. Fallback to settings.json
            var path = StoragePaths.SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.DeviceName) && !data.DeviceName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return data.DeviceName.Trim();
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
            var cleanName = deviceName.Trim();

            // 1. Save to INI in %APPDATA%\LanChat\config.ini
            IniConfigFile.WriteValue(StoragePaths.IniConfigFilePath, "General", "DeviceName", cleanName);

            // 2. Also try to update AppDir config.ini if accessible
            try
            {
                if (File.Exists(StoragePaths.AppDirIniConfigFilePath))
                {
                    IniConfigFile.WriteValue(StoragePaths.AppDirIniConfigFilePath, "General", "DeviceName", cleanName);
                }
            }
            catch { }

            // 3. Sync settings.json for compatibility
            var path = StoragePaths.SettingsFilePath;
            AppSettingsData data = new();
            if (File.Exists(path))
            {
                var existingJson = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<AppSettingsData>(existingJson) ?? new AppSettingsData();
            }
            data.DeviceName = cleanName;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public static string LoadLanguage()
    {
        try
        {
            // 1. Check %APPDATA%\LanChat\config.ini
            var iniLang = IniConfigFile.ReadValue(StoragePaths.IniConfigFilePath, "General", "Language");
            if (!string.IsNullOrWhiteSpace(iniLang))
            {
                var lang = iniLang!.Trim().ToLowerInvariant();
                if (lang == "id" || lang == "en") return lang;
            }

            // 2. Check AppDir config.ini (written by installer or bundled)
            var appDirIniLang = IniConfigFile.ReadValue(StoragePaths.AppDirIniConfigFilePath, "General", "Language");
            if (!string.IsNullOrWhiteSpace(appDirIniLang))
            {
                var lang = appDirIniLang!.Trim().ToLowerInvariant();
                if (lang == "id" || lang == "en")
                {
                    // Cache to AppData config.ini
                    IniConfigFile.WriteValue(StoragePaths.IniConfigFilePath, "General", "Language", lang);
                    return lang;
                }
            }

            // 3. Check legacy install_config.json
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

            // 4. Fallback settings.json
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

            // 5. Fallback OS culture
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
            var cleanLang = lang.ToLowerInvariant();

            // 1. Save to %APPDATA%\LanChat\config.ini
            IniConfigFile.WriteValue(StoragePaths.IniConfigFilePath, "General", "Language", cleanLang);

            // 2. Also save to AppDir config.ini if exists/writable
            try
            {
                if (File.Exists(StoragePaths.AppDirIniConfigFilePath))
                {
                    IniConfigFile.WriteValue(StoragePaths.AppDirIniConfigFilePath, "General", "Language", cleanLang);
                }
            }
            catch { }

            // 3. Save to settings.json
            var path = StoragePaths.SettingsFilePath;
            AppSettingsData data = new();
            if (File.Exists(path))
            {
                var existingJson = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<AppSettingsData>(existingJson) ?? new AppSettingsData();
            }
            data.Language = cleanLang;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
