using System;
using System.Collections.Generic;
using System.IO;

namespace LanChat.Core.Services;

/// <summary>
/// Lightweight, dependency-free INI configuration file reader and writer.
/// Preserves comments and structure when modifying or adding keys.
/// </summary>
public static class IniConfigFile
{
    private static readonly object _fileLock = new();

    public static string? ReadValue(string filePath, string section, string key)
    {
        lock (_fileLock)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var lines = File.ReadAllLines(filePath);
                string? currentSection = null;

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    if (string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                    {
                        var splitIdx = line.IndexOf('=');
                        if (splitIdx > 0)
                        {
                            var k = line.Substring(0, splitIdx).Trim();
                            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                            {
                                return line.Substring(splitIdx + 1).Trim();
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }
    }

    public static void WriteValue(string filePath, string section, string key, string value)
    {
        lock (_fileLock)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var lines = File.Exists(filePath)
                    ? new List<string>(File.ReadAllLines(filePath))
                    : new List<string>();

                int sectionStartIndex = -1;
                int sectionEndIndex = lines.Count;
                int keyIndex = -1;

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        var sec = line.Substring(1, line.Length - 2).Trim();
                        if (string.Equals(sec, section, StringComparison.OrdinalIgnoreCase))
                        {
                            sectionStartIndex = i;
                            // Find the end of this section or next section
                            for (int j = i + 1; j < lines.Count; j++)
                            {
                                var nextLine = lines[j].Trim();
                                if (nextLine.StartsWith("[") && nextLine.EndsWith("]"))
                                {
                                    sectionEndIndex = j;
                                    break;
                                }
                                var split = nextLine.IndexOf('=');
                                if (split > 0)
                                {
                                    var k = nextLine.Substring(0, split).Trim();
                                    if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                                    {
                                        keyIndex = j;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }

                string formattedEntry = $"{key}={value}";

                if (sectionStartIndex == -1)
                {
                    // Section not found; create standard commented section if file is empty
                    if (lines.Count == 0)
                    {
                        lines.Add("; ==============================================================================");
                        lines.Add("; LAN Chat Configuration File (.ini)");
                        lines.Add("; ==============================================================================");
                        lines.Add("; Language setting: en (English) | id (Indonesian)");
                        lines.Add("; DeviceName: custom display name (leave blank to use computer name)");
                        lines.Add("; ==============================================================================");
                        lines.Add(string.Empty);
                    }
                    else if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                    {
                        lines.Add(string.Empty);
                    }

                    lines.Add($"[{section}]");
                    lines.Add(formattedEntry);
                }
                else if (keyIndex != -1)
                {
                    // Key found inside section, replace line
                    lines[keyIndex] = formattedEntry;
                }
                else
                {
                    // Key not found in section, insert before sectionEndIndex
                    lines.Insert(sectionEndIndex, formattedEntry);
                }

                File.WriteAllLines(filePath, lines);
            }
            catch { }
        }
    }
}
