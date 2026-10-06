using System.Net;
using System.Text.Json;
using TANREN_Metsuke.Services;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Check that invalid files are rejected while valid zero-load sets remain usable.
public sealed class WorkoutDataTests
{
    [Theory]
    [InlineData("../2026-10-06.json")]
    [InlineData("2026-02-30.json")]
    [InlineData("0001-01-01.json")]
    [InlineData("settings.json")]
    [InlineData("2026-10-06.JSON")]
    public void InvalidFilenamesAreNotWorkouts(string filename) => Assert.False(WorkoutJson.IsWorkoutFilename(filename));

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"date\":\"2026-10-05\",\"entries\":[]}")]
    [InlineData("{\"date\":\"2026-10-06\",\"entries\":null}")]
    [InlineData("{\"date\":\"2026-10-06\",\"entries\":[{\"exerciseId\":\"bench_press\",\"sets\":null}]}")]
    [InlineData("{\"date\":\"2026-10-06\",\"entries\":[{\"exerciseId\":\"bench_press\",\"sets\":[{\"reps\":10}]}]}")]
    [InlineData("{\"date\":\"2026-10-06\",\"entries\":[null]}")]
    public void InvalidWorkoutsAreRejected(string json) => Assert.ThrowsAny<Exception>(() => WorkoutJson.ReadWorkout("2026-10-06.json", json));

    [Theory]
    [InlineData(-1, 30)]
    [InlineData(0, 30)]
    [InlineData(10, -1)]
    public void InvalidSetValuesAreRejected(int reps, double kg) =>
        Assert.Throws<InvalidDataException>(() => WorkoutJson.ReadWorkout("2026-10-06.json", TestData.Workout("2026-10-06", reps, kg)));

    [Fact]
    public void ZeroLoadIsValidAndEmptyEntriesAreIgnored()
    {
        var workout = WorkoutJson.ReadWorkout("2026-10-06.json", TestData.Workout("2026-10-06", 20, 0));
        Assert.Equal(20, workout.Entries[0].Sets[0].Reps);
        Assert.Equal(0, workout.TotalVolume);
        var empty = WorkoutJson.ReadWorkout("2026-10-06.json", "{\"date\":\"2026-10-06\",\"entries\":[{\"exerciseId\":\"bench_press\",\"sets\":[]}]}");
        Assert.Empty(empty.Entries);
    }

    [Fact]
    public void RepositorySkipsBadDataAndReportsIt()
    {
        using var data = new TestData();
        data.Write("2026-10-06.json", TestData.Workout("2026-10-06"));
        data.Write("2026-10-05.json", "{}");
        data.Write("2026-10-04.json", "{\"date\":\"2026-10-04\",\"entries\":null}");
        data.Write("unrelated.json", "{}");
        data.Write("custom_exercises.json", "[]");
        List<string> warnings = [];
        var sessions = new JsonWorkoutRepository(data.Workouts, warnings.Add).LoadAll();
        Assert.Single(sessions);
        Assert.Equal(300, sessions[0].TotalVolume);
        Assert.Equal(2, warnings.Count);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[{\"id\":\"custom_a\",\"name\":\"A\",\"primaryMuscles\":[\"unknown\"]}]")]
    [InlineData("[{\"id\":\"custom_a\",\"name\":\"A\",\"primaryMuscles\":[\"Chest\"],\"secondaryMuscles\":[\"Chest\"]}]")]
    [InlineData("[{\"id\":\"custom_a\",\"name\":\"A\",\"primaryMuscles\":[]}]")]
    public void InvalidCustomExercisesAreRejected(string json) => Assert.ThrowsAny<Exception>(() => WorkoutJson.ReadCustomExercises(json));

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("169.254.1.10", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("::1", false)]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.16.1.1", true)]
    [InlineData("172.32.1.1", false)]
    public void LanDetectionNeverAdvertisesLoopback(string address, bool usable) =>
        Assert.Equal(usable, LanAddressDetector.IsLanAddress(IPAddress.Parse(address)));
}
