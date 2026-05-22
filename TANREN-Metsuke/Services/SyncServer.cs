using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TANREN_Metsuke.Services;

// Main Sync service, istens for a single client connection from the phone, which sends a manifest of workout files it has
// the server responds with which files it needs, and the phone uploads them one by one,
// then saves them to disk and updates the UI status as it goes.
public class SyncServer : IDisposable
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    // workout JSON files are tiny, we cap the body to guard against a malformed or malicious Content-Length
    private const int MaxBodyBytes = 8 * 1024 * 1024;

    private readonly TcpListener listener;
    private readonly string token;
    private readonly X509Certificate2 certificate;
    private readonly Func<string> getFolder;
    private readonly Action<string> onStatus;
    private readonly Action onFileSaved;
    private readonly CancellationTokenSource cts = new();

    public int Port { get; }

    public SyncServer(string ip, string token, X509Certificate2 certificate, Func<string> getFolder, Action<string> onStatus, Action onFileSaved)
    {
        this.token = token;
        this.certificate = certificate;
        this.getFolder = getFolder;
        this.onStatus = onStatus;
        this.onFileSaved = onFileSaved;
        listener = new TcpListener(IPAddress.Parse(ip), 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public void StartAccepting() => Task.Run(() => AcceptLoopAsync(cts.Token));

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch (Exception) when (ct.IsCancellationRequested) { break; }
            catch (Exception) { /* ignore accept errors */ }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
    {
        using var _ = tcpClient;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        ct = timeout.Token;

        try
        {
            using var ssl = new SslStream(tcpClient.GetStream(), false);
            try
            {
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }, ct);
            }
            catch (Exception ex)
            {
                onStatus($"TLS handshake failed: {ex.Message}");
                return;
            }

            string method, path, body;
            Dictionary<string, string> headers;
            try
            {
                (method, path, headers, body) = await ParseRequestAsync(ssl, ct);
            }
            // only if Content-Length is too large
            catch (RequestTooLargeException)
            {
                await SendResponseAsync(ssl, 413, new { error = "Payload too large" }, ct);
                return;
            }

            var auth = headers.GetValueOrDefault("Authorization", "");
            if (auth != $"Bearer {token}")
            {
                await SendResponseAsync(ssl, 401, new { error = "Unauthorized" }, ct);
                return;
            }

            // follow the protocol between desktop and mobile:
            // GET /ping for connectivity check, POST /sync/manifest with the list of files on the phone,
            // then POST /sync/upload for each file the desktop needs, with the file content in the body
            if (method == "GET" && path == "/ping")
                await SendResponseAsync(ssl, 200, new { ok = true }, ct);
            else if (method == "POST" && path == "/sync/manifest")
                await HandleManifestAsync(ssl, body, ct);
            else if (method == "POST" && path == "/sync/upload")
                await HandleUploadAsync(ssl, body, ct);
            else
                await SendResponseAsync(ssl, 404, new { error = "Not found" }, ct);
        }
        catch (Exception) { /* ignore dropped connections and stream errors */ }
    }

    private async Task HandleManifestAsync(Stream stream, string body, CancellationToken ct)
    {
        onStatus("Phone connected, checking files...");

        var phoneFiles = JsonSerializer.Deserialize<List<FileMetadata>>(body, ReadOptions) ?? [];

        var folder = getFolder();
        Directory.CreateDirectory(folder);
        List<string> needed = [];

        foreach (var pf in phoneFiles)
        {
            var localPath = Path.Combine(folder, pf.Filename);
            if (!File.Exists(localPath))
            {
                needed.Add(pf.Filename);
                continue;
            }
            // if hash differs, the mobile need to reupload, maybe the file was modified on the phone since last sync
            if (ComputeHash(localPath) != pf.Hash)
                needed.Add(pf.Filename);
        }

        // remove local files the phone no longer has
        var phoneSet = new HashSet<string>(phoneFiles.Select(f => f.Filename), StringComparer.OrdinalIgnoreCase);
        var deleted = 0;
        foreach (var localFile in Directory.GetFiles(folder, "*.json"))
        {
            if (!phoneSet.Contains(Path.GetFileName(localFile)))
            {
                File.Delete(localFile);
                deleted++; //keep track to report in the status (response)
            }
        }

        if (deleted > 0)
            onFileSaved();

        var status = (needed.Count, deleted) switch
        {
            (0, 0) => "Already up to date",
            (0, _) => $"Removed {deleted} file(s) from desktop",
            (_, 0) => $"Requesting {needed.Count} file(s)...",
            _ => $"Requesting {needed.Count} file(s), removed {deleted}..."
        };
        onStatus(status);
        await SendResponseAsync(stream, 200, new { needed, deleted }, ct);
    }

    private async Task HandleUploadAsync(Stream stream, string body, CancellationToken ct)
    {
        var upload = JsonSerializer.Deserialize<FileUpload>(body, ReadOptions);

        if (upload == null || string.IsNullOrEmpty(upload.Filename))
        {
            await SendResponseAsync(stream, 400, new { error = "Missing filename" }, ct);
            return;
        }

        var safeName = Path.GetFileName(upload.Filename);
        if (string.IsNullOrEmpty(safeName) || safeName != upload.Filename)
        {
            await SendResponseAsync(stream, 400, new { error = "Invalid filename" }, ct);
            return;
        }

        var folder = getFolder();
        Directory.CreateDirectory(folder);

        var contentJson = upload.Content.GetRawText(); //data from mobile (it is ALREADY in json, no need to serialize again)
        await File.WriteAllTextAsync(Path.Combine(folder, safeName), contentJson, new UTF8Encoding(false), ct);

        onStatus($"Received {safeName}");
        await SendResponseAsync(stream, 200, new { ok = true }, ct);
        onFileSaved();
    }

    private static string ComputeHash(string path)
    {
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant(); //hash is SHA256 (mobile already computes this, we just compare)
    }

    private static async Task<(string Method, string Path, Dictionary<string, string> Headers, string Body)>
        ParseRequestAsync(Stream stream, CancellationToken ct)
    {
        var requestLine = await ReadLineAsync(stream, ct);
        var parts = requestLine.Split(' ', 3);
        var method = parts.Length > 0 ? parts[0] : "GET";
        var path = parts.Length > 1 ? parts[1] : "/";

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await ReadLineAsync(stream, ct);
            if (line == "")
                break;
            var colon = line.IndexOf(':');
            if (colon > 0)
                headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        var body = "";
        if (headers.TryGetValue("Content-Length", out var clStr) && int.TryParse(clStr, out var cl) && cl > 0)
        {
            if (cl > MaxBodyBytes)
                throw new RequestTooLargeException();
            var buf = new byte[cl];
            await stream.ReadExactlyAsync(buf, ct);
            body = Encoding.UTF8.GetString(buf);
        }

        return (method, path, headers, body);
    }

    private static async Task<string> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        while (true)
        {
            await stream.ReadExactlyAsync(buf, ct);
            if (buf[0] == '\n')
                break;
            if (buf[0] != '\r')
                sb.Append((char)buf[0]);
        }
        return sb.ToString();
    }

    private static async Task SendResponseAsync(Stream stream, int status, object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var statusText = status switch { 200 => "OK", 400 => "Bad Request", 401 => "Unauthorized", 413 => "Payload Too Large", _ => "Error" };
        var header = $"HTTP/1.1 {status} {statusText}\r\nContent-Type: application/json\r\nContent-Length: {jsonBytes.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(header), ct);
        await stream.WriteAsync(jsonBytes, ct);
        await stream.FlushAsync(ct);
    }

    public void Dispose()
    {
        cts.Cancel();
        listener.Stop();
        GC.SuppressFinalize(this);
    }
}

file class RequestTooLargeException : Exception;

file record FileMetadata(string Filename, long Modified, string Hash);

file class FileUpload
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    [JsonPropertyName("content")]
    public JsonElement Content { get; set; }
}
