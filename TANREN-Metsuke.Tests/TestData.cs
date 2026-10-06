using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TANREN_Metsuke.Services;
using Xunit;

// The exercise catalog and chart configuration are shared, keep tests from changing them at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TANREN_Metsuke.Tests;

// Keep synthetic workout files beneath the test output, never use the user's app data.
internal sealed class TestData : IDisposable
{
    public string Root { get; } = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
    public string Workouts => Path.Combine(Root, "workouts");
    public TestData() => Directory.CreateDirectory(Workouts);
    public void Write(string name, string json) => File.WriteAllText(Path.Combine(Workouts, name), json, new UTF8Encoding(false));
    public static string Workout(string date, int reps = 10, double kg = 30) =>
        JsonSerializer.Serialize(new { date, entries = new[] { new { exerciseId = "bench_press", sets = new[] { new { reps, kg } } } } });
    // Build manifest hashes from the same UTF-8 content that the upload will contain.
    public static SyncFileMetadata Metadata(string name, string json) => new(name, 0, Hash(json));
    public static string Hash(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    public static JsonElement Content(string json) => JsonDocument.Parse(json).RootElement.Clone();
    public void Dispose()
    {
        // Remove only the unique synthetic folder created by this test instance.
        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
