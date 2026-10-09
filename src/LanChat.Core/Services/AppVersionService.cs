using System;
using System.IO;
using System.Reflection;

namespace LanChat.Core.Services;

public static class AppVersionService
{
    public static string Version { get; } = LoadVersion();

    private static string LoadVersion()
    {
        try
        {
            // 1. Try reading embedded resource version.conf (bundled inside assembly, works everywhere including Android)
            var asm = typeof(AppVersionService).Assembly;
            using (var stream = asm.GetManifestResourceStream("LanChat.Core.version.conf"))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("VERSION=", StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmed.Substring("VERSION=".Length).Trim();
                        }
                        if (!trimmed.StartsWith("#") && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.Contains("="))
                        {
                            return trimmed;
                        }
                    }
                }
            }

            // 2. Try file system search (useful in development/local execution)
            var searchDirs = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (var startDir in searchDirs)
            {
                if (string.IsNullOrEmpty(startDir)) continue;

                var dir = new DirectoryInfo(startDir);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var file = Path.Combine(dir.FullName, "version.conf");
                    if (File.Exists(file))
                    {
                        foreach (var line in File.ReadAllLines(file))
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("VERSION=", StringComparison.OrdinalIgnoreCase))
                            {
                                return trimmed.Substring("VERSION=".Length).Trim();
                            }
                            if (!trimmed.StartsWith("#") && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.Contains("="))
                            {
                                return trimmed;
                            }
                        }
                    }
                    dir = dir.Parent;
                }
            }

            // 3. Try assembly InformationalVersion or Version
            var infoVer = asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVer))
            {
                return infoVer.Split('+')[0].Trim();
            }

            var asmVer = asm.GetName().Version;
            if (asmVer != null && (asmVer.Major > 0 || asmVer.Minor > 0 || asmVer.Build > 0))
            {
                return $"{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}";
            }
        }
        catch { }

        return "0.3.13";
    }
}
