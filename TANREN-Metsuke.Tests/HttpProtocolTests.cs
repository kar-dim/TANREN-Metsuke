using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using TANREN_Metsuke.Services;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Exercise the real HTTP parser and transfer handlers without depending on Windows TLS services.
public sealed class HttpProtocolTests
{
    // Separate input and output buffers model a connection carrying one or more HTTP requests.
    private sealed class DuplexStream(string requests) : Stream
    {
        private readonly MemoryStream input = new(Encoding.UTF8.GetBytes(requests));
        private readonly MemoryStream output = new();
        public string Response => Encoding.UTF8.GetString(output.ToArray());
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => input.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => input.ReadAsync(buffer, ct);
        public override void Write(byte[] buffer, int offset, int count) => output.Write(buffer, offset, count);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => output.WriteAsync(buffer, ct);
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { input.Dispose(); output.Dispose(); base.Dispose(disposing); }
    }

    // Content-Length uses the byte count because JSON can include non-ASCII text.
    private static string Request(string path, string body = "", string token = "test-token", string extraHeaders = "", string method = "POST") =>
        $"{method} {path} HTTP/1.1\r\nHost: localhost\r\nAuthorization: Bearer {token}\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\n{extraHeaders}\r\n{body}";

    private static async Task<string> Send(SyncServer server, string requests)
    {
        using var stream = new DuplexStream(requests);
        await server.HandleHttpConnectionAsync(stream, CancellationToken.None);
        return stream.Response;
    }
    private static string ResponseBody(string response) => response[(response.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..];
    private static X509Certificate2 Certificate()
    {
        using var key = RSA.Create(2048);
        return new CertificateRequest("CN=Protocol Test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"protocolVersion\":2,\"complete\":false,\"files\":[]}")]
    [InlineData("{\"protocolVersion\":\"2\",\"complete\":true,\"files\":[]}")]
    public async Task InvalidManifestsReturnErrorsWithoutDeletingFiles(string body)
    {
        using var data = new TestData();
        var json = TestData.Workout("2026-10-06");
        data.Write("2026-10-06.json", json);
        using var cert = Certificate();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => { });
        Assert.StartsWith("HTTP/1.1 400", await Send(server, Request("/sync/manifest", body)));
        Assert.Equal(json, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public async Task ProtocolCommitsAndReloadsOnceAcrossConnectionsAndDuplicateCompletion()
    {
        using var data = new TestData();
        using var cert = Certificate();
        var reloads = 0;
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => reloads++);
        var first = TestData.Workout("2026-10-05");
        var second = TestData.Workout("2026-10-06");
        var manifest = JsonSerializer.Serialize(new { protocolVersion = 2, complete = true, files = new[] { TestData.Metadata("2026-10-05.json", first), TestData.Metadata("2026-10-06.json", second) } });
        var response = await Send(server, Request("/sync/manifest", manifest));
        Assert.StartsWith("HTTP/1.1 200", response);
        var id = JsonDocument.Parse(ResponseBody(response)).RootElement.GetProperty("sessionId").GetString()!;
        foreach (var (name, json) in new[] { ("2026-10-05.json", first), ("2026-10-06.json", second) })
        {
            var upload = $"{{\"sessionId\":\"{id}\",\"filename\":\"{name}\",\"content\":{json}}}";
            Assert.StartsWith("HTTP/1.1 200", await Send(server, Request("/sync/upload", upload)));
            Assert.Equal(0, reloads);
            Assert.Empty(new JsonWorkoutRepository(data.Workouts).LoadAll());
        }
        Assert.StartsWith("HTTP/1.1 401", await Send(server, Request("/ping", token: "invalid", method: "GET")));
        Assert.Equal(0, reloads);
        var complete = Request("/sync/complete", JsonSerializer.Serialize(new { sessionId = id }));
        Assert.StartsWith("HTTP/1.1 200", await Send(server, complete));
        Assert.StartsWith("HTTP/1.1 200", await Send(server, complete));
        Assert.Equal(1, reloads);
        Assert.Equal(2, new JsonWorkoutRepository(data.Workouts).LoadAll().Count);
    }

    [Fact]
    public async Task KeepAliveProcessesSeveralHttpRequestsInOneStream()
    {
        using var data = new TestData();
        using var cert = Certificate();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => { });
        var ping = Request("/ping", method: "GET");
        var response = await Send(server, ping + ping);
        Assert.Equal(2, response.Split("HTTP/1.1 200", StringSplitOptions.None).Length - 1);
        Assert.Contains("Connection: keep-alive", response);
    }

    [Theory]
    [InlineData("Content-Length: 1\r\n", 400)]
    [InlineData("Transfer-Encoding: chunked\r\n", 400)]
    [InlineData("Expect: 100-continue\r\n", 400)]
    public async Task UnsupportedOrAmbiguousHttpFramingIsRejected(string extraHeaders, int status)
    {
        using var data = new TestData();
        using var cert = Certificate();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => { });
        Assert.StartsWith($"HTTP/1.1 {status}", await Send(server, Request("/ping", extraHeaders: extraHeaders, method: "GET")));
    }

    [Fact]
    public async Task OversizedBodyIsRejectedBeforeAllocationOrRead()
    {
        using var data = new TestData();
        using var cert = Certificate();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => { });
        Assert.StartsWith("HTTP/1.1 413", await Send(server, "POST /sync/upload HTTP/1.1\r\nContent-Length: 8388609\r\n\r\n"));
    }

    [Fact]
    public async Task ShutdownRetainsLiveDataAndReportsBlockedStagingCleanup()
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06", kg: 20);
        var changed = TestData.Workout("2026-10-06", kg: 50);
        data.Write("2026-10-06.json", original);
        using var cert = Certificate();
        var statuses = new List<string>();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, statuses.Add, () => { });
        var manifest = JsonSerializer.Serialize(new { protocolVersion = 2, complete = true, files = new[] { TestData.Metadata("2026-10-06.json", changed) } });
        var response = await Send(server, Request("/sync/manifest", manifest));
        using var document = JsonDocument.Parse(ResponseBody(response));
        var id = document.RootElement.GetProperty("sessionId").GetString()!;
        var upload = $"{{\"sessionId\":\"{id}\",\"filename\":\"2026-10-06.json\",\"content\":{changed}}}";
        Assert.StartsWith("HTTP/1.1 200", await Send(server, Request("/sync/upload", upload)));
        var staged = Path.Combine(data.Root, ".workouts-sync", "uploads", "2026-10-06.json");
        using (File.Open(staged, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            server.Dispose();
            if (OperatingSystem.IsWindows())
            {
                Assert.True(File.Exists(staged));
                Assert.Contains(statuses, status => status.Contains("Recovery files have been retained."));
            }
        }
        Assert.Equal(original, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }
}
