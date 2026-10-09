using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LanChat.Core.Services;

/// <summary>
/// Provides a persistent, unique hardware/system device identity (GUID)
/// that remains stable across app restarts, uninstalls, re-installs, and device name changes.
/// </summary>
public static class DeviceIdentityService
{
    private static string? _cachedDeviceId;

    public static string GetOrCreateDeviceId()
    {
        if (!string.IsNullOrEmpty(_cachedDeviceId))
            return _cachedDeviceId;

        // 1. Check OS hardware / installation machine identifier first
        // (Persistent in Windows Registry even if app is uninstalled and installed again)
        string? osMachineId = GetOsMachineId();
        if (!string.IsNullOrWhiteSpace(osMachineId))
        {
            _cachedDeviceId = FormatDeviceId(osMachineId!);
            SaveToConfig(_cachedDeviceId);
            return _cachedDeviceId;
        }

        // 2. Check persistent config.ini if already saved previously
        var savedIni = AppSettingsService.LoadDeviceId();
        if (!string.IsNullOrWhiteSpace(savedIni))
        {
            _cachedDeviceId = FormatDeviceId(savedIni);
            return _cachedDeviceId;
        }

        // 3. Fallback: Generate a fresh 128-bit GUID and persist it
        var newGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
        _cachedDeviceId = newGuid;
        SaveToConfig(newGuid);
        return _cachedDeviceId;
    }

    private static string? GetOsMachineId()
    {
        try
        {
            // Windows: MachineGuid from Registry (HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using var key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                                 .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    var val = key64?.GetValue("MachineGuid")?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val!.Trim();
                }
                catch { }

                try
                {
                    using var key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                                                 .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    var val = key32?.GetValue("MachineGuid")?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val!.Trim();
                }
                catch { }
            }
            // Linux: /etc/machine-id or /var/lib/dbus/machine-id
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    if (File.Exists("/etc/machine-id"))
                    {
                        var id = File.ReadAllText("/etc/machine-id").Trim();
                        if (!string.IsNullOrWhiteSpace(id)) return id;
                    }
                    if (File.Exists("/var/lib/dbus/machine-id"))
                    {
                        var id = File.ReadAllText("/var/lib/dbus/machine-id").Trim();
                        if (!string.IsNullOrWhiteSpace(id)) return id;
                    }
                }
                catch { }
            }
        }
        catch { }

        return null;
    }

    private static string FormatDeviceId(string rawId)
    {
        var cleaned = rawId.Trim().Trim('{', '}').ToUpperInvariant();
        if (cleaned.Length == 32 && !cleaned.Contains("-"))
        {
            if (Guid.TryParse(cleaned, out var g))
            {
                return g.ToString("D").ToUpperInvariant();
            }
        }
        return cleaned;
    }

    private static void SaveToConfig(string deviceId)
    {
        try
        {
            AppSettingsService.SaveDeviceId(deviceId);
        }
        catch { }
    }
}
