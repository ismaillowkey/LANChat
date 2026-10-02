using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanChat.Core.Services;

public static class NetworkUtils
{
    public static List<IPAddress> GetLocalIPv4Addresses()
    {
        var list = new List<IPAddress>();
        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = iface.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        list.Add(addr.Address);
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }

        if (list.Count == 0)
        {
            list.Add(IPAddress.Loopback);
        }

        return list;
    }

    public static List<IPAddress> GetBroadcastAddresses()
    {
        var broadcastList = new List<IPAddress> { IPAddress.Broadcast };

        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = iface.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork &&
                        unicast.IPv4Mask != null)
                    {
                        var ipBytes = unicast.Address.GetAddressBytes();
                        var maskBytes = unicast.IPv4Mask.GetAddressBytes();
                        var broadcastBytes = new byte[ipBytes.Length];

                        for (int i = 0; i < ipBytes.Length; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        }

                        var broadcastIp = new IPAddress(broadcastBytes);
                        if (!broadcastList.Contains(broadcastIp))
                        {
                            broadcastList.Add(broadcastIp);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignored, default 255.255.255.255 already included
        }

        return broadcastList;
    }

    public static string GetLocalMacAddress()
    {
        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus == OperationalStatus.Up &&
                    iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var macBytes = iface.GetPhysicalAddress().GetAddressBytes();
                    if (macBytes != null && macBytes.Length == 6)
                    {
                        return string.Join(":", macBytes.Select(b => b.ToString("X2")));
                    }
                }
            }
        }
        catch { }
        return "00:00:00:00:00:00";
    }
}
