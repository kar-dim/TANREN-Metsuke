using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TANREN_Metsuke.Services;

// Keep the adapter name with its address so users can choose the network shared with their phone.
public sealed record LanAddress(string Ip, string AdapterName, bool Physical)
{
    public string Display => $"{AdapterName}: {Ip}";
}

// Find usable private IPv4 addresses instead of advertising a loopback fallback.
public static class LanAddressDetector
{
    // Prefer Ethernet and Wi-Fi, other private adapters remain available for manual selection.
    public static List<LanAddress> GetAddresses() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.PrefixLength is >= 1 and <= 30 && IsLanAddress(a.Address))
            .Select(a => new LanAddress(a.Address.ToString(), n.Name,
                n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)))
        .DistinctBy(a => a.Ip).OrderByDescending(a => a.Physical).ThenBy(a => a.AdapterName, StringComparer.OrdinalIgnoreCase).ToList();

    public static bool IsLanAddress(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;
        // Limit QR addresses to the three private IPv4 ranges used by local networks.
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168);
    }
}
