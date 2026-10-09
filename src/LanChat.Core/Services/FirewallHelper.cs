using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LanChat.Core.Services;

public static class FirewallHelper
{
    public const string RuleNameApp = "LAN Chat";
    public const string RuleNameUdp = "LAN Chat (UDP Discovery)";
    public const string RuleNameTcp = "LAN Chat (TCP Transport)";

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsFirewallRulePresent()
    {
        if (!IsWindows) return true;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{RuleNameApp}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd();
            proc?.WaitForExit(2000);
            return output != null && output.Contains(RuleNameApp);
        }
        catch
        {
            return false;
        }
    }

    public static void AutoConfigureOnStartup()
    {
        if (!IsWindows) return;

        Task.Run(() =>
        {
            try
            {
                if (IsFirewallRulePresent()) return;

                // 1. Try silent registration
                EnsureFirewallRulesSilent();
                if (IsFirewallRulePresent()) return;

                // 2. If running portable without prior setup, automatically prompt Windows UAC once
                RequestElevatedFirewallAccess();
            }
            catch { }
        });
    }

    public static void EnsureFirewallRulesSilent()
    {
        if (!IsWindows) return;

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            // Try adding rules silently (succeeds if app runs with admin rights)
            RunNetshSilent($"advfirewall firewall add rule name=\"{RuleNameApp}\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any");
            RunNetshSilent($"advfirewall firewall add rule name=\"{RuleNameUdp}\" dir=in action=allow protocol=UDP localport=45450 enable=yes profile=any");
            RunNetshSilent($"advfirewall firewall add rule name=\"{RuleNameTcp}\" dir=in action=allow protocol=TCP localport=45451-45500 enable=yes profile=any");
        }
        catch { }
    }

    public static bool RequestElevatedFirewallAccess()
    {
        if (!IsWindows) return false;

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return false;

            // Create temporary .bat script that runs elevated via runas (UAC prompt)
            var tempBat = Path.Combine(Path.GetTempPath(), "lanchat_allow_firewall.bat");
            var batContent = $@"@echo off
netsh advfirewall firewall add rule name=""{RuleNameApp}"" dir=in action=allow program=""{exePath}"" enable=yes profile=any
netsh advfirewall firewall add rule name=""{RuleNameUdp}"" dir=in action=allow protocol=UDP localport=45450 enable=yes profile=any
netsh advfirewall firewall add rule name=""{RuleNameTcp}"" dir=in action=allow protocol=TCP localport=45451-45500 enable=yes profile=any
del ""%~f0""
";
            File.WriteAllText(tempBat, batContent);

            var psi = new ProcessStartInfo
            {
                FileName = tempBat,
                UseShellExecute = true,
                Verb = "runas", // Triggers Windows UAC Administrator prompt
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to request elevated firewall access: {ex.Message}");
            return false;
        }
    }

    private static void RunNetshSilent(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch { }
    }
}
