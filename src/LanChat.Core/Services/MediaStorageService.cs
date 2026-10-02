namespace LanChat.Core.Services;

public static class MediaStorageService
{
    public static string MediaDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LanChat", "Media");

    public static string SaveMedia(string originalFileName, byte[] data)
    {
        try
        {
            var dir = MediaDirectory;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var safeName = Path.GetFileName(originalFileName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = $"media_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
            }

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
}
