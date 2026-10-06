using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TANREN_Metsuke.Services;

// TLS HTTP/1.1 with bounded clients and keep-alive. Batch lifetime is independent of connections.
public sealed class SyncServer : IDisposable
{
    public const int ProtocolVersion = 2;
    private const int MaxBodyBytes = 8 * 1024 * 1024;
    private const int MaxLineBytes = 8 * 1024;
    private const int MaxHeaderCount = 100;
    private readonly TcpListener listener;
    private readonly byte[] authorization;
    private readonly X509Certificate2 certificate;
    private readonly Action<string> onStatus;
    private readonly Action onSyncCompleted;
    private readonly CancellationTokenSource cts = new();
    private readonly SemaphoreSlim clients = new(8);
    private readonly SemaphoreSlim requests = new(1);
    private readonly SyncBatchStore store;
    private Task? accepting;
    private int disposed;

    public int Port { get; }

    public SyncServer(string ip, string token, X509Certificate2 certificate, Func<string> getFolder, Action<string> onStatus, Action onSyncCompleted)
    {
        authorization = Encoding.UTF8.GetBytes($"Bearer {token}");
        this.certificate = certificate;
        this.onStatus = onStatus;
        this.onSyncCompleted = onSyncCompleted;
        store = new SyncBatchStore(getFolder());
        listener = new TcpListener(IPAddress.Parse(ip), 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public void StartAccepting() => accepting ??= Task.Run(() => AcceptLoopAsync(cts.Token));

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await clients.WaitAsync(ct);
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(ct); }
                catch { clients.Release(); throw; }
                _ = HandleClientAsync(client, ct);
            }
        }
        catch (Exception) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { onStatus($"Sync listener stopped: {ex.Message}. Refresh the connection to retry."); }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken serverToken)
    {
        using var client = tcpClient;
        try
        {
            using var ssl = new SslStream(client.GetStream(), false);
            using (var handshake = CancellationTokenSource.CreateLinkedTokenSource(serverToken))
            {
                handshake.CancelAfter(TimeSpan.FromSeconds(10));
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }, handshake.Token);
            }
            using var buffered = new BufferedStream(ssl, 8192);
            await HandleHttpConnectionAsync(buffered, serverToken);
        }
        catch (AuthenticationException ex) { onStatus($"TLS handshake failed: {ex.Message}. Refresh the connection and scan the new QR code."); }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException or ObjectDisposedException) { }
        catch (Exception ex) { onStatus($"Sync request failed: {ex.Message}"); }
        finally { clients.Release(); }
    }

    internal async Task HandleHttpConnectionAsync(Stream stream, CancellationToken serverToken)
    {
        while (!serverToken.IsCancellationRequested)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var ct = timeout.Token;
            HttpRequest? request;
            try { request = await ParseRequestAsync(stream, ct); }
            catch (RequestTooLargeException)
            {
                await SendResponseAsync(stream, 413, new { error = "Payload too large" }, false, ct);
                return;
            }
            catch (InvalidDataException ex)
            {
                await SendResponseAsync(stream, 400, new { error = ex.Message }, false, ct);
                return;
            }
            if (request == null)
                return;
            var keepAlive = !request.Headers.GetValueOrDefault("Connection", "").Equals("close", StringComparison.OrdinalIgnoreCase);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(request.Headers.GetValueOrDefault("Authorization", "")), authorization))
            {
                await SendResponseAsync(stream, 401, new { error = "Unauthorized" }, false, ct);
                return;
            }
            await ProcessRequestAsync(stream, request.Method, request.Path, request.Body, keepAlive, ct);
            if (!keepAlive)
                return;
        }
    }

    private async Task ProcessRequestAsync(Stream stream, string method, string path, string body, bool keepAlive, CancellationToken ct)
    {
        await requests.WaitAsync(ct);
        try
        {
            try
            {
                if (method == "GET" && path == "/ping")
                    await SendResponseAsync(stream, 200, new { ok = true, protocolVersion = ProtocolVersion }, keepAlive, ct);
                else if (method == "POST" && path == "/sync/manifest")
                {
                    using var document = JsonDocument.Parse(body);
                    var root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object ||
                        !root.TryGetProperty("protocolVersion", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != ProtocolVersion ||
                        !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                        !root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
                        throw new InvalidDataException("Sync requires protocolVersion 2 and a complete file manifest. Update TANREN Kiroku.");
                    var metadata = JsonSerializer.Deserialize<List<SyncFileMetadata>>(files.GetRawText(), JsonDefaults.CaseInsensitive)!;
                    var result = store.Begin(metadata);
                    onStatus($"Requesting {result.Needed.Length} file(s). Desktop data is unchanged until transfer completes.");
                    await SendResponseAsync(stream, 200, new { sessionId = result.SessionId, needed = result.Needed, deleted = result.Deleted }, keepAlive, ct);
                }
                else if (method == "POST" && path == "/sync/upload")
                {
                    var upload = JsonSerializer.Deserialize<FileUpload>(body, JsonDefaults.CaseInsensitive)
                        ?? throw new InvalidDataException("Missing upload.");
                    store.Upload(upload.SessionId, upload.Filename, upload.Content);
                    onStatus($"Received {upload.Filename}; waiting for transfer completion.");
                    await SendResponseAsync(stream, 200, new { ok = true }, keepAlive, ct);
                }
                else if (method == "POST" && path == "/sync/complete")
                {
                    var completion = JsonSerializer.Deserialize<CompletionRequest>(body, JsonDefaults.CaseInsensitive)
                        ?? throw new InvalidDataException("Missing completion request.");
                    var changed = store.Complete(completion.SessionId);
                    onStatus(changed ? "Sync complete! Previous data saved in sync-backups." : "Sync complete, desktop is up to date.");
                    if (changed)
                        onSyncCompleted();
                    await SendResponseAsync(stream, 200, new { ok = true }, keepAlive, ct);
                }
                else
                    await SendResponseAsync(stream, 404, new { error = "Not found" }, keepAlive, ct);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or InvalidOperationException)
            {
                await SendResponseAsync(stream, ex is InvalidOperationException ? 409 : 400, new { error = ex.Message }, keepAlive, ct);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                onStatus($"Sync storage error: {ex.Message}");
                await SendResponseAsync(stream, 500, new { error = "Could not save the transfer. Retry sync; the previous desktop snapshot is retained." }, keepAlive, ct);
            }
        }
        finally { requests.Release(); }
    }

    private static async Task<HttpRequest?> ParseRequestAsync(Stream stream, CancellationToken ct)
    {
        var requestLine = await ReadLineAsync(stream, ct);
        if (requestLine == null)
            return null;
        var parts = requestLine.Split(' ');
        if (parts.Length != 3 || parts[2] != "HTTP/1.1")
            throw new InvalidDataException("Expected an HTTP/1.1 request.");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var count = 0; ; count++)
        {
            var line = await ReadLineAsync(stream, ct) ?? throw new InvalidDataException("Incomplete headers.");
            if (line.Length == 0)
                break;
            if (count >= MaxHeaderCount)
                throw new RequestTooLargeException();
            var colon = line.IndexOf(':');
            if (colon <= 0 || !headers.TryAdd(line[..colon].Trim(), line[(colon + 1)..].Trim()))
                throw new InvalidDataException("Invalid or duplicate header.");
        }
        if (headers.ContainsKey("Transfer-Encoding") || headers.ContainsKey("Expect"))
            throw new InvalidDataException("Use Content-Length without chunked encoding or Expect.");
        var length = 0;
        if (headers.TryGetValue("Content-Length", out var value) && (!int.TryParse(value, out length) || length < 0))
            throw new InvalidDataException("Invalid Content-Length.");
        if (length > MaxBodyBytes)
            throw new RequestTooLargeException();
        if (parts[0] == "POST" && length == 0)
            throw new InvalidDataException("POST requests require a JSON body and Content-Length.");
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, ct);
        return new HttpRequest(parts[0], parts[1], headers, Encoding.UTF8.GetString(bytes));
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var text = new StringBuilder();
        var buffer = new byte[1];
        var count = 0;
        while (true)
        {
            if (await stream.ReadAsync(buffer, ct) == 0)
                return count == 0 ? null : throw new InvalidDataException("Incomplete HTTP line.");
            if (++count > MaxLineBytes)
                throw new RequestTooLargeException();
            if (buffer[0] == '\n')
                return text.ToString();
            if (buffer[0] != '\r')
                text.Append((char)buffer[0]);
        }
    }

    private static async Task SendResponseAsync(Stream stream, int status, object body, bool keepAlive, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(body);
        var reason = status switch { 200 => "OK", 400 => "Bad Request", 401 => "Unauthorized", 404 => "Not Found", 409 => "Conflict", 413 => "Payload Too Large", _ => "Internal Server Error" };
        var header = $"HTTP/1.1 {status} {reason}\r\nContent-Type: application/json\r\nContent-Length: {json.Length}\r\nConnection: {(keepAlive ? "keep-alive" : "close")}\r\n\r\n";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(header), ct);
        await stream.WriteAsync(json, ct);
        await stream.FlushAsync(ct);
    }

    private sealed record HttpRequest(string Method, string Path, Dictionary<string, string> Headers, string Body);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        cts.Cancel();
        listener.Stop();
        requests.Wait();
        try { store.Dispose(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leave recovery files in place when cleanup is blocked by a file lock or permissions.
            onStatus($"Sync cleanup could not finish: {ex.Message}. Recovery files have been retained.");
        }
        finally { requests.Release(); }
    }
}

file sealed class RequestTooLargeException : Exception;
file sealed class FileUpload
{
    public string SessionId { get; set; } = "";
    public string Filename { get; set; } = "";
    public JsonElement Content { get; set; }
}
file sealed class CompletionRequest
{
    public string SessionId { get; set; } = "";
}
