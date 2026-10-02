using System.Text.Json;

namespace LanChat.Core.Services;

public class AppSettingsData
{
    public string DeviceName { get; set; } = string.Empty;
}

public static class AppSettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LanChat");
    private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "settings.json");

    public static string LoadDeviceName(string defaultName)
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
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
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            var data = new AppSettingsData { DeviceName = deviceName };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }
}
