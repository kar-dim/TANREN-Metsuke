using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            var token = Guid.NewGuid().ToString("N")[..16];
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
        string? fallback = null;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // ignore common virtual interfaces and loopback, and prefer LAN IPs in case of multiple addresses
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.Name.StartsWith("vEthernet", StringComparison.OrdinalIgnoreCase))
                continue;
            if (ni.Name.StartsWith("Loopback", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (addr.PrefixLength >= 32) // may be a VPN tunnel
                    continue;

                var ip = addr.Address.ToString();
                if (ip.StartsWith("192.168.")) // most probable to be the correct one local IP
                    return ip;
                fallback ??= ip;
            }
        }
        return fallback ?? "127.0.0.1";
    }
}
