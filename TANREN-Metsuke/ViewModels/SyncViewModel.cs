using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;
using ReactiveUI;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

public sealed class SyncViewModel : ViewModelBase, IDisposable
{
    private readonly Func<string> getFolder;
    private readonly Func<Task> onSyncCompleted;
    private readonly SemaphoreSlim refreshGate = new(1);
    private SyncServer? server;
    private X509Certificate2? certificate;
    private int generation;
    private bool disposed;

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    private List<LanAddress> addresses = [];
    public List<LanAddress> Addresses
    {
        get => addresses;
        private set => this.RaiseAndSetIfChanged(ref addresses, value);
    }
    private LanAddress? selectedAddress;
    public LanAddress? SelectedAddress
    {
        get => selectedAddress;
        set => this.RaiseAndSetIfChanged(ref selectedAddress, value);
    }
    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        private set => this.RaiseAndSetIfChanged(ref isRefreshing, value);
    }
    private Bitmap? qrBitmap;
    public Bitmap? QrBitmap
    {
        get => qrBitmap;
        private set => this.RaiseAndSetIfChanged(ref qrBitmap, value);
    }
    private string statusText = "Finding local network...";
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

    public SyncViewModel(Func<string> getFolder, Func<Task> onSyncCompleted)
    {
        this.getFolder = getFolder;
        this.onSyncCompleted = onSyncCompleted;
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        await refreshGate.WaitAsync();
        try
        {
            if (disposed)
                return;
            IsRefreshing = true;
            IsReady = false;
            ConnectionInfo = "";
            var oldBitmap = QrBitmap;
            QrBitmap = null;
            oldBitmap?.Dispose();
            var currentGeneration = Interlocked.Increment(ref generation);
            var oldServer = server;
            var oldCertificate = certificate;
            server = null;
            certificate = null;
            await Task.Run(() => DisposeConnection(oldServer, oldCertificate));
            var selectedIp = SelectedAddress?.Ip;
            var found = await Task.Run(LanAddressDetector.GetAddresses);
            if (disposed)
                return;
            Addresses = found;
            SelectedAddress = found.FirstOrDefault(a => a.Ip == selectedIp) ?? found.FirstOrDefault();
            if (SelectedAddress == null)
            {
                StatusText = "No local network address found. Connect to Wi-Fi or Ethernet, then refresh.";
                return;
            }
            var ip = SelectedAddress.Ip;
            var token = RandomNumberGenerator.GetHexString(32, lowercase: true);
            var connection = await Task.Run(() =>
            {
                var cert = CertificateManager.LoadOrCreate();
                try
                {
                    var syncServer = new SyncServer(ip, token, cert, getFolder,
                        msg => Dispatcher.UIThread.Post(() =>
                        {
                            if (!disposed && generation == currentGeneration)
                                StatusText = msg;
                        }),
                        () => Dispatcher.UIThread.Post(async () =>
                        {
                            if (!disposed && generation == currentGeneration)
                                await onSyncCompleted();
                        }));
                    return (Server: syncServer, Certificate: cert);
                }
                catch { cert.Dispose(); throw; }
            });
            if (disposed)
            {
                DisposeConnection(connection.Server, connection.Certificate);
                return;
            }
            server = connection.Server;
            certificate = connection.Certificate;
            QrBitmap = GenerateQr(ip, server.Port, token, CertificateManager.Fingerprint(certificate));
            server.StartAccepting();
            ConnectionInfo = $"{SelectedAddress.AdapterName} — {ip}:{server.Port}";
            StatusText = "Waiting for phone...";
            IsReady = true;
        }
        catch (Exception ex)
        {
            DisposeConnection(server, certificate);
            server = null;
            certificate = null;
            if (!disposed)
                StatusText = $"Could not start sync: {ex.Message}. Refresh to retry.";
        }
        finally
        {
            if (!disposed)
                IsRefreshing = false;
            refreshGate.Release();
        }
    }

    private void OnNetworkChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (disposed || IsRefreshing)
            return;
        IsReady = false;
        StatusText = "Network addresses changed. Refresh the connection and scan the new QR code.";
    });

    private static Bitmap GenerateQr(string ip, int port, string token, string cert)
    {
        var payload = JsonSerializer.Serialize(new { ip, port, token, cert, protocolVersion = SyncServer.ProtocolVersion });
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        using var stream = new MemoryStream(png.GetGraphic(10));
        return new Bitmap(stream);
    }

    private static void DisposeConnection(SyncServer? syncServer, X509Certificate2? cert)
    {
        try { syncServer?.Dispose(); }
        finally { cert?.Dispose(); }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        Interlocked.Increment(ref generation);
        DisposeConnection(server, certificate);
        server = null;
        certificate = null;
        QrBitmap?.Dispose();
        RefreshCommand.Dispose();
    }
}
