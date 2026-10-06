using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using TANREN_Metsuke.Services;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Run complete HTTPS transfers with certificate pinning and real local sockets.
[Trait("Category", "TlsIntegration")]
public sealed class SyncServerTests
{
    private static X509Certificate2 Certificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test Sync", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Schannel on Windows requires a CNG key container, as does the production certificate loader.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
    }

    private static HttpClient Client(SyncServer server, X509Certificate2 cert, Action? onHandshake = null)
    {
        // Trust only this test server's certificate, matching the phone's QR pinning behavior.
        var pin = SHA256.HashData(cert.RawData);
        var handler = new HttpClientHandler
        {
            SslProtocols = SslProtocols.Tls12,
            ServerCertificateCustomValidationCallback = (_, remote, _, _) =>
            {
                onHandshake?.Invoke();
                return remote != null && CryptographicOperations.FixedTimeEquals(pin, SHA256.HashData(remote.RawData));
            }
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri($"https://127.0.0.1:{server.Port}"), Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        return client;
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string path, string body) =>
        client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task TlsTransferReloadsExactlyOnceAndReusesConnection()
    {
        using var data = new TestData();
        using var cert = Certificate();
        var reloads = 0;
        var handshakes = 0;
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => Interlocked.Increment(ref reloads));
        server.StartAccepting();
        using var client = Client(server, cert, () => Interlocked.Increment(ref handshakes));
        using var ping = await client.GetAsync("/ping");
        ping.EnsureSuccessStatusCode();
        Assert.Equal(2, JsonDocument.Parse(await ping.Content.ReadAsStringAsync()).RootElement.GetProperty("protocolVersion").GetInt32());
        var first = TestData.Workout("2026-10-05");
        var second = TestData.Workout("2026-10-06");
        var manifest = JsonSerializer.Serialize(new { protocolVersion = 2, complete = true, files = new[] { TestData.Metadata("2026-10-05.json", first), TestData.Metadata("2026-10-06.json", second) } });
        using var response = await Post(client, "/sync/manifest", manifest);
        response.EnsureSuccessStatusCode();
        var id = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("sessionId").GetString()!;
        foreach (var (name, json) in new[] { ("2026-10-05.json", first), ("2026-10-06.json", second) })
        {
            using var upload = await Post(client, "/sync/upload", $"{{\"sessionId\":\"{id}\",\"filename\":\"{name}\",\"content\":{json}}}");
            upload.EnsureSuccessStatusCode();
            Assert.Equal(0, Volatile.Read(ref reloads));
            Assert.Empty(new JsonWorkoutRepository(data.Workouts).LoadAll());
        }
        using var completed = await Post(client, "/sync/complete", JsonSerializer.Serialize(new { sessionId = id }));
        completed.EnsureSuccessStatusCode();
        Assert.Equal(1, Volatile.Read(ref reloads));
        Assert.Equal(2, new JsonWorkoutRepository(data.Workouts).LoadAll().Count);
        using var duplicate = await Post(client, "/sync/complete", JsonSerializer.Serialize(new { sessionId = id }));
        duplicate.EnsureSuccessStatusCode();
        Assert.Equal(1, Volatile.Read(ref reloads));
        Assert.Equal(1, Volatile.Read(ref handshakes));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"protocolVersion\":2,\"complete\":false,\"files\":[]}")]
    [InlineData("{\"protocolVersion\":1,\"complete\":true,\"files\":[]}")]
    [InlineData("{\"protocolVersion\":\"2\",\"complete\":true,\"files\":[]}")]
    public async Task InvalidManifestsNeverDeleteData(string body)
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06");
        data.Write("2026-10-06.json", original);
        using var cert = Certificate();
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => { });
        server.StartAccepting();
        using var client = Client(server, cert);
        using var response = await Post(client, "/sync/manifest", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public async Task UnauthorizedRequestsCannotInterruptAnAuthenticatedBatch()
    {
        using var data = new TestData();
        using var cert = Certificate();
        var reloads = 0;
        using var server = new SyncServer("127.0.0.1", "test-token", cert, () => data.Workouts, _ => { }, () => Interlocked.Increment(ref reloads));
        server.StartAccepting();
        using var client = Client(server, cert);
        var json = TestData.Workout("2026-10-06");
        using var manifest = await Post(client, "/sync/manifest", JsonSerializer.Serialize(new { protocolVersion = 2, complete = true, files = new[] { TestData.Metadata("2026-10-06.json", json) } }));
        manifest.EnsureSuccessStatusCode();
        var id = JsonDocument.Parse(await manifest.Content.ReadAsStringAsync()).RootElement.GetProperty("sessionId").GetString()!;
        using var unauthorized = Client(server, cert);
        unauthorized.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-token");
        using var rejected = await unauthorized.GetAsync("/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        using var upload = await Post(client, "/sync/upload", $"{{\"sessionId\":\"{id}\",\"filename\":\"2026-10-06.json\",\"content\":{json}}}");
        upload.EnsureSuccessStatusCode();
        Assert.Equal(0, Volatile.Read(ref reloads));
        using var completion = await Post(client, "/sync/complete", JsonSerializer.Serialize(new { sessionId = id }));
        completion.EnsureSuccessStatusCode();
        Assert.Equal(1, Volatile.Read(ref reloads));
    }
}
