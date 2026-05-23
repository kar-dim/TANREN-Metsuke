using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;
using ReactiveUI;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// ViewModel for the sync page, which starts a local server and generates a QR code for the mobile app to connect and sync workout data
public class SyncViewModel : ViewModelBase
{
    private readonly Func<string> getFolder;
    private SyncServer? server;

    private Bitmap? qrBitmap;
    public Bitmap? QrBitmap
    {
        get => qrBitmap;
        private set => this.RaiseAndSetIfChanged(ref qrBitmap, value);
    }

    private string statusText = "Starting server...";
    public string StatusText
    {
        get => statusText;
        private set => this.RaiseAndSetIfChanged(ref statusText, value);
    }

    private string connectionInfo = "";
    public string ConnectionInfo
    {
        get => connectionInfo;
        private set => this.RaiseAndSetIfChanged(ref connectionInfo, value);
    }

    private bool isReady;
    public bool IsReady
    {
        get => isReady;
        private set => this.RaiseAndSetIfChanged(ref isReady, value);
    }

    public SyncViewModel(Func<string> getFolder, Action onFileSaved)
    {
        this.getFolder = getFolder;
        StartServer(onFileSaved);
    }

    // start the server and generate random QR code for the session
    // the mobile app will scan the QR and provide us with its content
    private void StartServer(Action onFileSaved)
    {
        try
        {
            var ip = DetectLocalIp();
            var token = RandomNumberGenerator.GetHexString(32, lowercase: true); // 128-bit
            var cert = CertificateManager.LoadOrCreate();
            var fingerprint = CertificateManager.Fingerprint(cert);

            server = new SyncServer(
                ip: ip,
                token: token,
                certificate: cert,
                getFolder: getFolder,
                onStatus: msg => Dispatcher.UIThread.Post(() => StatusText = msg),
                onFileSaved: () => Dispatcher.UIThread.Post(onFileSaved)
            );
            server.StartAccepting();

            ConnectionInfo = $"{ip}:{server.Port}";
            QrBitmap = GenerateQr(ip, server.Port, token, fingerprint);
            StatusText = "Waiting for phone...";
            IsReady = true;
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to start server: {ex.Message}";
            IsReady = false;
        }
    }

    private static Bitmap GenerateQr(string ip, int port, string token, string cert)
    {
        var payload = JsonSerializer.Serialize(new { ip, port, token, cert });
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10);
        using var ms = new MemoryStream(bytes);
        return new Bitmap(ms);
    }

    private static string DetectLocalIp()
    {
        // pick the physical LAN interface, not the default route or VPN interface
        // first pass restricts to wired or wireless adapters, the second pass relaxes that so a real NIC
        // reporting an unusual type is still found (NOTE: the gateway and subnet checks still exclude VPNs and virtual adapters)
        return ScanForLan(requireKnownType: true) ?? ScanForLan(requireKnownType: false) ?? "127.0.0.1";
    }

    private static string? ScanForLan(bool requireKnownType)
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (requireKnownType && ni.NetworkInterfaceType is not (NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211))
                continue;
            var props = ni.GetIPProperties();
            // virtual and host only adapters have no gateway
            if (!props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)))
                continue;
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (addr.PrefixLength is < 1 or > 30) // /32 has NO local subnet, mostly for WireGuard style VPN tunnels
                    continue;
                if (IsUsableLan(addr.Address))
                    return addr.Address.ToString();
            }
        }
        return null;
    }

    // private ranges, excluding APIPA (169.254) and loopback
    private static bool IsUsableLan(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }
}
