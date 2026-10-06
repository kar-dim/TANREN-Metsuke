using TANREN_Metsuke.Services;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Check staged transfers, backups and crash recovery using disposable workout folders.
public sealed class SyncBatchTests
{
    [Fact]
    public void HashingPreservesUtf8AndEscapedCustomExerciseNames()
    {
        using var data = new TestData();
        using var store = new SyncBatchStore(data.Workouts);
        const string json = "[{\"id\":\"custom_a\",\"name\":\"Άσκηση \\\"A\\\"\",\"primaryMuscles\":[\"Chest\"],\"secondaryMuscles\":[]}]";
        var batch = store.Begin([TestData.Metadata("custom_exercises.json", json)]);
        store.Upload(batch.SessionId, "custom_exercises.json", TestData.Content(json));
        Assert.True(store.Complete(batch.SessionId));
        Assert.Equal(json, File.ReadAllText(Path.Combine(data.Workouts, "custom_exercises.json")));
        Assert.Equal("Άσκηση \"A\"", WorkoutJson.ReadCustomExercises(json)[0].Name);
    }

    [Fact]
    public void FailedSnapshotCopyPreservesOriginalAndAllowsRetry()
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06", kg: 20);
        var changed = TestData.Workout("2026-10-06", kg: 40);
        data.Write("2026-10-06.json", original);
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([TestData.Metadata("2026-10-06.json", changed)]);
        store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(changed));
        data.Write("locked.txt", "file open during commit");
        using (var locked = File.Open(Path.Combine(data.Workouts, "locked.txt"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<IOException>(() => store.Complete(batch.SessionId));
            Assert.Equal(original, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
        }
        Assert.True(store.Complete(batch.SessionId));
        Assert.Equal(changed, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public void DuplicateManifestEntriesAndInvalidHashesAreRejected()
    {
        using var data = new TestData();
        using var store = new SyncBatchStore(data.Workouts);
        var entry = TestData.Metadata("2026-10-06.json", TestData.Workout("2026-10-06"));
        Assert.Throws<InvalidDataException>(() => store.Begin([entry, entry]));
        Assert.Throws<InvalidDataException>(() => store.Begin([entry with { Hash = "bad" }]));
        Assert.Empty(new JsonWorkoutRepository(data.Workouts).LoadAll());
    }

    [Fact]
    public void FilesThatChangedAfterManifestRequireAFreshSnapshot()
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06", kg: 20);
        data.Write("2026-10-06.json", original);
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([TestData.Metadata("2026-10-06.json", original)]);
        Assert.Empty(batch.Needed);
        data.Write("2026-10-06.json", TestData.Workout("2026-10-06", kg: 40));
        Assert.Throws<InvalidDataException>(() => store.Complete(batch.SessionId));
        Assert.Equal(400, new JsonWorkoutRepository(data.Workouts).LoadAll()[0].TotalVolume);
    }

    [Fact]
    public void UploadsAndDeletionsOnlyBecomeVisibleAtCompletion()
    {
        using var data = new TestData();
        var old = TestData.Workout("2026-10-04");
        data.Write("2026-10-04.json", old);
        data.Write("notes.txt", "keep me");
        var first = TestData.Workout("2026-10-05");
        var second = TestData.Workout("2026-10-06");
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([TestData.Metadata("2026-10-05.json", first), TestData.Metadata("2026-10-06.json", second)]);
        Assert.Equal(1, batch.Deleted);
        store.Upload(batch.SessionId, "2026-10-05.json", TestData.Content(first));
        Assert.True(File.Exists(Path.Combine(data.Workouts, "2026-10-04.json")));
        Assert.False(File.Exists(Path.Combine(data.Workouts, "2026-10-05.json")));
        Assert.Throws<InvalidOperationException>(() => store.Complete(batch.SessionId));
        store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(second));
        Assert.Single(new JsonWorkoutRepository(data.Workouts).LoadAll());
        Assert.True(store.Complete(batch.SessionId));
        Assert.Equal(2, new JsonWorkoutRepository(data.Workouts).LoadAll().Count);
        Assert.False(File.Exists(Path.Combine(data.Workouts, "2026-10-04.json")));
        Assert.Equal("keep me", File.ReadAllText(Path.Combine(data.Workouts, "notes.txt")));
        var backup = Assert.Single(Directory.GetDirectories(Path.Combine(data.Root, "sync-backups")));
        Assert.Equal(old, File.ReadAllText(Path.Combine(backup, "2026-10-04.json")));
        Assert.False(store.Complete(batch.SessionId));
    }

    [Fact]
    public void InterruptedTransferRetainsOriginalData()
    {
        using var data = new TestData();
        var old = TestData.Workout("2026-10-06", kg: 20);
        data.Write("2026-10-06.json", old);
        var changed = TestData.Workout("2026-10-06", kg: 50);
        using (var store = new SyncBatchStore(data.Workouts))
        {
            var batch = store.Begin([TestData.Metadata("2026-10-06.json", changed)]);
            store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(changed));
        }
        Assert.Equal(old, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public void HashMismatchUnrequestedUploadAndExpiredSessionsCannotModifyData()
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06");
        data.Write("2026-10-06.json", original);
        var changed = TestData.Workout("2026-10-06", kg: 50);
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([TestData.Metadata("2026-10-06.json", changed)]);
        Assert.Throws<InvalidDataException>(() => store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(original)));
        Assert.Throws<InvalidOperationException>(() => store.Upload(batch.SessionId, "2026-10-05.json", TestData.Content(TestData.Workout("2026-10-05"))));
        var replacement = store.Begin([TestData.Metadata("2026-10-06.json", changed)]);
        Assert.Throws<InvalidOperationException>(() => store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(changed)));
        Assert.Throws<InvalidOperationException>(() => store.Complete(replacement.SessionId));
        Assert.Equal(original, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Theory]
    [InlineData("../2026-10-06.json")]
    [InlineData("custom_exercises.JSON")]
    [InlineData("2026-02-30.json")]
    public void ManifestPathsAreRejectedBeforeAnyDeletion(string name)
    {
        using var data = new TestData();
        var original = TestData.Workout("2026-10-06");
        data.Write("2026-10-06.json", original);
        using var store = new SyncBatchStore(data.Workouts);
        Assert.Throws<InvalidDataException>(() => store.Begin([TestData.Metadata(name, original)]));
        Assert.True(File.Exists(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public void InvalidContentIsRejectedEvenWhenItsHashMatches()
    {
        using var data = new TestData();
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([TestData.Metadata("2026-10-06.json", "{}")]);
        Assert.Throws<InvalidDataException>(() => store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content("{}")));
        Assert.Throws<InvalidOperationException>(() => store.Complete(batch.SessionId));
        Assert.Empty(new JsonWorkoutRepository(data.Workouts).LoadAll());
    }

    [Fact]
    public void DuplicateUploadsAndUnchangedManifestsAreIdempotent()
    {
        using var data = new TestData();
        using var store = new SyncBatchStore(data.Workouts);
        var json = TestData.Workout("2026-10-06");
        var batch = store.Begin([TestData.Metadata("2026-10-06.json", json)]);
        store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(json));
        store.Upload(batch.SessionId, "2026-10-06.json", TestData.Content(json));
        Assert.True(store.Complete(batch.SessionId));
        var next = store.Begin([TestData.Metadata("2026-10-06.json", json)]);
        Assert.Empty(next.Needed);
        Assert.False(store.Complete(next.SessionId));
        Assert.Single(Directory.GetDirectories(Path.Combine(data.Root, "sync-backups")));
    }

    [Fact]
    public void ExplicitEmptyDatasetIsAppliedOnlyOnCompletionAndHasBackup()
    {
        using var data = new TestData();
        data.Write("2026-10-06.json", TestData.Workout("2026-10-06"));
        data.Write("custom_exercises.json", "[]");
        using var store = new SyncBatchStore(data.Workouts);
        var batch = store.Begin([]);
        Assert.Equal(2, batch.Deleted);
        Assert.Single(new JsonWorkoutRepository(data.Workouts).LoadAll());
        Assert.True(store.Complete(batch.SessionId));
        Assert.Empty(new JsonWorkoutRepository(data.Workouts).LoadAll());
        Assert.Single(Directory.GetDirectories(Path.Combine(data.Root, "sync-backups")));
    }

    [Fact]
    public void RestartRecoversDirectorySwapInterruptedBeforeReplacement()
    {
        using var data = new TestData();
        var json = TestData.Workout("2026-10-06");
        data.Write("2026-10-06.json", json);
        var transaction = Path.Combine(data.Root, ".workouts-sync");
        Directory.CreateDirectory(transaction);
        Directory.Move(data.Workouts, Path.Combine(transaction, "previous"));
        var workouts = new JsonWorkoutRepository(data.Workouts).LoadAll();
        Assert.Single(workouts);
        Assert.Equal(json, File.ReadAllText(Path.Combine(data.Workouts, "2026-10-06.json")));
    }

    [Fact]
    public void RestartArchivesPreviousSnapshotAfterSuccessfulSwap()
    {
        using var data = new TestData();
        var transaction = Path.Combine(data.Root, ".workouts-sync");
        var previous = Path.Combine(transaction, "previous");
        Directory.CreateDirectory(previous);
        File.WriteAllText(Path.Combine(previous, "2026-10-05.json"), TestData.Workout("2026-10-05"));
        data.Write("2026-10-06.json", TestData.Workout("2026-10-06"));
        Assert.Single(new JsonWorkoutRepository(data.Workouts).LoadAll());
        var backup = Assert.Single(Directory.GetDirectories(Path.Combine(data.Root, "sync-backups")));
        Assert.True(File.Exists(Path.Combine(backup, "2026-10-05.json")));
    }
}
