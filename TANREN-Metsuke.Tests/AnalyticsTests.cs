using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;
using TANREN_Metsuke.ViewModels;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Check chart totals, date ranges and records against small workouts with known results.
public sealed class AnalyticsTests
{
    static AnalyticsTests() => LiveCharts.Configure(config => config.AddSkiaSharp().AddDefaultMappers());
    private static WorkoutEntry Entry(string id, int reps = 10, double kg = 30) => new() { ExerciseId = id, Sets = [new() { Reps = reps, Kg = kg }] };
    private static WorkoutSession Session(params WorkoutEntry[] entries) => new() { Date = DateOnly.FromDateTime(DateTime.Today), Entries = entries.ToList() };
    private static double? ExerciseValue(GraphsViewModel graph) => Assert.IsType<LineSeries<DateTimePoint>>(Assert.Single(graph.ExerciseSeries)).Values!.Single().Value;

    [Fact]
    public void SevenDaysContainsExactlySevenDatesAndExcludesFutureDates()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sessions = Enumerable.Range(-1, 9).Select(i => new WorkoutSession { Date = today.AddDays(-i), Entries = [Entry("bench_press")] }).ToList();
        var graph = new GraphsViewModel(sessions);
        graph.IsRange7d = true;
        var points = Assert.IsType<ColumnSeries<DateTimePoint>>(Assert.Single(graph.WorkoutSeries)).Values!.ToArray();
        Assert.Equal(7, points.Length);
        Assert.All(points, p => Assert.InRange(DateOnly.FromDateTime(p.DateTime), today.AddDays(-6), today));
    }

    [Fact]
    public void DuplicateExerciseEntriesAreAggregatedForAllMetrics()
    {
        var graph = new GraphsViewModel([Session(Entry("bench_press", 10, 30), Entry("bench_press", 12, 50))]);
        Assert.Equal(50, ExerciseValue(graph));
        graph.IsVolume = true;
        Assert.Equal(900, ExerciseValue(graph));
        graph.IsMaxReps = true;
        Assert.Equal(12, ExerciseValue(graph));
    }

    [Fact]
    public void EmptySetListsNeverCrashChartInitialization()
    {
        var graph = new GraphsViewModel([Session(new WorkoutEntry { ExerciseId = "bench_press", Sets = [] })]);
        Assert.False(graph.HasExerciseData);
        Assert.Empty(graph.AvailableExercises);
        Assert.Empty(graph.ExerciseSeries);
    }

    [Fact]
    public void RadarDeduplicatesMusclesWithinEachSpoke()
    {
        var row = Entry("barbell_row");
        row.Sets.AddRange([new() { Reps = 10, Kg = 30 }, new() { Reps = 10, Kg = 30 }]);
        var graph = new GraphsViewModel([Session(row)], secondaryWeight: 0.5);
        var primary = Assert.IsType<PolarLineSeries<double>>(graph.RadarSeries.Last()).Values!.ToArray();
        var combined = Assert.IsType<PolarLineSeries<double>>(graph.RadarSeries.First()).Values!.ToArray();
        Assert.Equal(3, primary[4]); // Back + Traps are one spoke.
        Assert.Equal(3, combined[4]);
        Assert.Equal(1.5, combined[2]); // Biceps + Forearms count once as secondary Arms.
    }

    [Fact]
    public void UnitsAndReloadPreserveSelectionsAndChartTab()
    {
        var graph = new GraphsViewModel([Session(Entry("bench_press"), Entry("dumbbell_curl"))]);
        graph.SelectedExercise = graph.AvailableExercises.Single(e => e.Id == "dumbbell_curl");
        graph.IsMaxReps = true;
        graph.IsRangeCustom = true;
        graph.CustomStartDate = DateTime.Today.AddDays(-20);
        graph.CustomEndDate = DateTime.Today;
        graph.SelectedChartTabIndex = 2;
        graph.UpdateImperial(true);
        graph.UpdateSessions([Session(Entry("bench_press"), Entry("dumbbell_curl", 15, 10))]);
        Assert.Equal("dumbbell_curl", graph.SelectedExercise!.Id);
        Assert.True(graph.IsMaxReps);
        Assert.True(graph.IsRangeCustom);
        Assert.Equal(DateTime.Today.AddDays(-20), graph.CustomStartDate);
        Assert.Equal(2, graph.SelectedChartTabIndex);
        Assert.True(graph.Imperial);
        Assert.Equal(15, ExerciseValue(graph));
    }

    [Fact]
    public void ZeroLoadSetsHaveRepRecordsAndRadarSetsButNoLoadRecords()
    {
        var sessions = new List<WorkoutSession> { Session(Entry("push_up", 20, 0)) };
        var record = Assert.Single(PersonalRecordCalculator.Compute(sessions)[MuscleGroup.Chest]);
        Assert.Equal(20, record.BestReps.Reps);
        Assert.Null(record.BestWeight);
        Assert.Null(record.BestSet);
        Assert.Null(record.BestSession);
        Assert.Contains("no added weight", record.BestRepsDisplay);
        Assert.Equal(0, sessions[0].TotalVolume);
        var graph = new GraphsViewModel(sessions);
        Assert.True(graph.HasRadarData);
        Assert.Equal(1, Assert.IsType<PolarLineSeries<double>>(graph.RadarSeries.Last()).Values!.ElementAt(1));
        Assert.Equal(1, new RecordsViewModel(sessions).TotalExercisesTracked);
    }

    [Fact]
    public void RecordsDistinguishBestSetMostRepsAndFullSessionVolume()
    {
        var sessions = new List<WorkoutSession> { Session(Entry("bench_press", 10, 30), Entry("bench_press", 8, 50), Entry("bench_press", 20, 0)) };
        var record = Assert.Single(PersonalRecordCalculator.Compute(sessions)[MuscleGroup.Chest]);
        Assert.Equal(50, record.BestWeight!.Kg);
        Assert.Equal(400, record.BestSet!.Volume);
        Assert.Equal(20, record.BestReps.Reps);
        Assert.Equal(700, record.BestSession!.Volume);
        var viewModel = new RecordsViewModel(sessions);
        viewModel.UpdateImperial(true);
        Assert.Contains("lb", viewModel.GetRecords(MuscleGroup.Chest)[0].BestWeightDisplay);
        Assert.Equal(50, viewModel.GetRecords(MuscleGroup.Chest)[0].BestWeight!.Kg);
    }
}
