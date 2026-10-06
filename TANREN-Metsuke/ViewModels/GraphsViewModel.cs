using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using ReactiveUI;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;
using TANREN_Metsuke.Theme;

namespace TANREN_Metsuke.ViewModels;

public enum ChartMetric { MaxWeight, MaxReps, Volume }
public enum ChartRange { Week, Month, ThreeMonths, SixMonths, All, Custom }

// View model for the graphs tab, responsible for preparing data and configuration for the various charts displayed in that tab
public class GraphsViewModel : ViewModelBase
{
    private static readonly SKColor AccentColor = SKColor.Parse(Palette.Accent);
    private static readonly SKColor SecondaryColor = SKColor.Parse(Palette.Secondary);
    private static readonly SKColor CardBg = SKColor.Parse(Palette.CardBg);
    private static readonly SKColor TextMuted = SKColor.Parse(Palette.TextMuted);
    private static readonly SKColor TextCaption = SKColor.Parse(Palette.TextCaption);
    private static readonly SKColor DividerBrush = SKColor.Parse(Palette.Divider);

    private static readonly (string Label, MuscleGroup[] Muscles)[] RadarSpokes =
    [
        ("Shoulders", [MuscleGroup.Shoulders]),
        ("Chest", [MuscleGroup.Chest]),
        ("Arms", [MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Forearms]),
        ("Core", [MuscleGroup.Core]),
        ("Back", [MuscleGroup.Back, MuscleGroup.Traps]),
        ("Legs", [MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Calves, MuscleGroup.Shins]),
        ("Glutes", [MuscleGroup.Glutes]),
    ];

    private List<WorkoutSession> sessions;
    private Dictionary<string, List<(DateOnly Date, List<WorkoutSet> Sets)>> exerciseHistory = [];
    private Dictionary<DateOnly, double> sessionVolumes = [];
    private readonly Axis exerciseYAxis;
    private bool imperial;
    private double secondaryWeight;
    private ChartMetric metric;
    private ChartRange range = ChartRange.Month;
    private WorkoutSession? pendingClickedSession;

    public bool Imperial => imperial;
    public int SelectedChartTabIndex { get; set; }

    public event Action<WorkoutSession>? WorkoutSessionClicked;

    public bool IsRange7d { get => range == ChartRange.Week; set { if (value) SetRange(ChartRange.Week); } }
    public bool IsRange1m { get => range == ChartRange.Month; set { if (value) SetRange(ChartRange.Month); } }
    public bool IsRange3m { get => range == ChartRange.ThreeMonths; set { if (value) SetRange(ChartRange.ThreeMonths); } }
    public bool IsRange6m { get => range == ChartRange.SixMonths; set { if (value) SetRange(ChartRange.SixMonths); } }
    public bool IsRangeAll { get => range == ChartRange.All; set { if (value) SetRange(ChartRange.All); } }
    public bool IsRangeCustom { get => range == ChartRange.Custom; set { if (value) SetRange(ChartRange.Custom); } }

    private DateTime? customStartDate;
    public DateTime? CustomStartDate
    {
        get => customStartDate;
        set
        {
            this.RaiseAndSetIfChanged(ref customStartDate, value);
            if (range == ChartRange.Custom)
                RefreshAllCharts();
        }
    }

    private DateTime? customEndDate;
    public DateTime? CustomEndDate
    {
        get => customEndDate;
        set
        {
            this.RaiseAndSetIfChanged(ref customEndDate, value);
            if (range == ChartRange.Custom)
                RefreshAllCharts();
        }
    }

    private void SetRange(ChartRange r)
    {
        range = r;
        this.RaisePropertyChanged(nameof(IsRange7d));
        this.RaisePropertyChanged(nameof(IsRange1m));
        this.RaisePropertyChanged(nameof(IsRange3m));
        this.RaisePropertyChanged(nameof(IsRange6m));
        this.RaisePropertyChanged(nameof(IsRangeAll));
        this.RaisePropertyChanged(nameof(IsRangeCustom));
        RefreshAllCharts();
    }

    public void RefreshAllCharts()
    {
        var filtered = FilteredSessions();
        UpdateExerciseChart(filtered);
        UpdateWorkoutChart(filtered);
        UpdateWeeklyChart(filtered);
        UpdateRadarChart(filtered);
    }

    private List<WorkoutSession> FilteredSessions()
    {
        if (range == ChartRange.Custom)
        {
            var start = customStartDate.HasValue ? DateOnly.FromDateTime(customStartDate.Value) : DateOnly.MinValue;
            var end = customEndDate.HasValue ? DateOnly.FromDateTime(customEndDate.Value) : DateOnly.MaxValue;
            return sessions.Where(s => s.Date >= start && s.Date <= end).ToList();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = range switch
        {
            ChartRange.Week => today.AddDays(-6),
            ChartRange.Month => today.AddMonths(-1),
            ChartRange.ThreeMonths => today.AddMonths(-3),
            ChartRange.SixMonths => today.AddMonths(-6),
            _ => DateOnly.MinValue
        };
        return sessions.Where(s => s.Date >= cutoff && (range == ChartRange.All || s.Date <= today)).ToList();
    }

    // By Exercise
    private List<ExerciseDefinition> availableExercises = [];
    public List<ExerciseDefinition> AvailableExercises
    {
        get => availableExercises;
        private set => this.RaiseAndSetIfChanged(ref availableExercises, value);
    }

    private ExerciseDefinition? selectedExercise;
    public ExerciseDefinition? SelectedExercise
    {
        get => selectedExercise;
        set { this.RaiseAndSetIfChanged(ref selectedExercise, value); UpdateExerciseChart(FilteredSessions()); }
    }

    public bool IsMaxWeight { get => metric == ChartMetric.MaxWeight; set { if (value) SetMetric(ChartMetric.MaxWeight); } }
    public bool IsMaxReps { get => metric == ChartMetric.MaxReps; set { if (value) SetMetric(ChartMetric.MaxReps); } }
    public bool IsVolume { get => metric == ChartMetric.Volume; set { if (value) SetMetric(ChartMetric.Volume); } }

    private void SetMetric(ChartMetric m)
    {
        metric = m;
        this.RaisePropertyChanged(nameof(IsMaxWeight));
        this.RaisePropertyChanged(nameof(IsMaxReps));
        this.RaisePropertyChanged(nameof(IsVolume));
        UpdateExerciseChart(FilteredSessions());
    }

    private ISeries[] exerciseSeries = [];
    public ISeries[] ExerciseSeries
    {
        get => exerciseSeries;
        private set => this.RaiseAndSetIfChanged(ref exerciseSeries, value);
    }

    private bool hasExerciseData;
    public bool HasExerciseData
    {
        get => hasExerciseData;
        private set => this.RaiseAndSetIfChanged(ref hasExerciseData, value);
    }

    public Axis[] ExerciseXAxes { get; }
    public Axis[] ExerciseYAxes { get; }

    // By Workout
    private ISeries[] workoutSeries = [];
    public ISeries[] WorkoutSeries
    {
        get => workoutSeries;
        private set => this.RaiseAndSetIfChanged(ref workoutSeries, value);
    }

    private bool hasWorkoutData;
    public bool HasWorkoutData
    {
        get => hasWorkoutData;
        private set => this.RaiseAndSetIfChanged(ref hasWorkoutData, value);
    }

    public Axis[] WorkoutXAxes { get; }
    public Axis[] WorkoutYAxes { get; }

    // Weekly
    private ISeries[] weeklySeries = [];
    public ISeries[] WeeklySeries
    {
        get => weeklySeries;
        private set => this.RaiseAndSetIfChanged(ref weeklySeries, value);
    }

    private bool hasWeeklyData;
    public bool HasWeeklyData
    {
        get => hasWeeklyData;
        private set => this.RaiseAndSetIfChanged(ref hasWeeklyData, value);
    }

    public Axis[] WeeklyXAxes { get; }
    public Axis[] WeeklyYAxes { get; }

    // Muscle Radar
    private ISeries[] radarSeries = [];
    public ISeries[] RadarSeries
    {
        get => radarSeries;
        private set => this.RaiseAndSetIfChanged(ref radarSeries, value);
    }

    private bool hasRadarData;
    public bool HasRadarData
    {
        get => hasRadarData;
        private set => this.RaiseAndSetIfChanged(ref hasRadarData, value);
    }

    public bool HasSecondary => secondaryWeight > 0;

    public PolarAxis[] RadarAngleAxes { get; }
    public PolarAxis[] RadarRadiusAxes { get; }

    // initialize the view model (all charts are initialized here)
    public GraphsViewModel(List<WorkoutSession> sessions, bool imperial = false, double secondaryWeight = 0)
    {
        this.sessions = sessions.OrderBy(s => s.Date).ToList();
        this.imperial = imperial;
        this.secondaryWeight = secondaryWeight;

        IndexSessions();

        var labelPaint = new SolidColorPaint(TextMuted);
        var gridPaint = new SolidColorPaint(DividerBrush);
        var captionPaint = new SolidColorPaint(TextCaption);

        var dayTicks = TimeSpan.FromDays(1).Ticks;
        var weekTicks = TimeSpan.FromDays(7).Ticks;
        var volumeName = $"Volume ({WeightHelper.Unit(imperial)})";

        // axes initialization per chart type
        ExerciseXAxes = [MakeDateAxis("dd MMM yy", dayTicks, labelPaint, gridPaint)];
        exerciseYAxis = MakeVolumeAxis("", labelPaint, gridPaint, captionPaint);
        ExerciseYAxes = [exerciseYAxis];

        WorkoutXAxes = [MakeDateAxis("dd MMM yy", dayTicks, labelPaint, gridPaint)];
        WorkoutYAxes = [MakeVolumeAxis(volumeName, labelPaint, gridPaint, captionPaint)];

        WeeklyXAxes = [MakeDateAxis("d MMM yy", weekTicks, labelPaint, gridPaint)];
        WeeklyYAxes = [MakeVolumeAxis(volumeName, labelPaint, gridPaint, captionPaint)];

        RadarAngleAxes =
        [
            new PolarAxis
            {
                Labels = [.. RadarSpokes.Select(s => s.Label)],
                TextSize = 13,
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint,
            }
        ];

        RadarRadiusAxes =
        [
            new PolarAxis
            {
                MinLimit = 0,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(TextCaption),
                SeparatorsPaint = new SolidColorPaint(DividerBrush),
                Labeler = v => $"{v:F0} sets",
            }
        ];

        customEndDate = DateTime.Today;
        customStartDate = sessions.Count > 0
            ? sessions.First().Date.ToDateTime(TimeOnly.MinValue)
            : DateTime.Today.AddMonths(-1);

        selectedExercise = AvailableExercises.FirstOrDefault();
        RefreshAllCharts();
    }

    private void IndexSessions()
    {
        exerciseHistory = sessions.SelectMany(s => s.Entries.Where(e => e.Sets.Count > 0).Select(e => (s.Date, Entry: e)))
            .GroupBy(x => x.Entry.ExerciseId)
            .ToDictionary(g => g.Key, g => g.GroupBy(x => x.Date)
                .Select(day => (day.Key, day.SelectMany(x => x.Entry.Sets).ToList())).OrderBy(x => x.Key).ToList());
        sessionVolumes = sessions.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.Sum(s => s.TotalVolume));
        AvailableExercises = exerciseHistory.Keys.Select(id => ExerciseCatalog.Get(id) ?? new ExerciseDefinition { Id = id, Name = id })
            .OrderBy(e => e.Name).ToList();
    }

    public void UpdateSessions(List<WorkoutSession> updated)
    {
        var selectedId = selectedExercise?.Id;
        sessions = updated.OrderBy(s => s.Date).ToList();
        pendingClickedSession = null;
        IndexSessions();
        selectedExercise = AvailableExercises.FirstOrDefault(e => e.Id == selectedId) ?? AvailableExercises.FirstOrDefault();
        this.RaisePropertyChanged(nameof(SelectedExercise));
        RefreshAllCharts();
    }

    public void UpdateImperial(bool useImperial)
    {
        if (imperial == useImperial)
            return;
        imperial = useImperial;
        this.RaisePropertyChanged(nameof(Imperial));
        WorkoutYAxes[0].Name = WeeklyYAxes[0].Name = $"Load Volume ({WeightHelper.Unit(imperial)})";
        RefreshAllCharts();
    }

    // cleared on pointer down and after a pan, we fire the click only when the press was a real click
    public void ClearPendingWorkoutClick() => pendingClickedSession = null;

    public void CommitWorkoutClickIfPending()
    {
        if (pendingClickedSession is { } session)
        {
            pendingClickedSession = null;
            WorkoutSessionClicked?.Invoke(session);
        }
    }

    public void UpdateSecondaryWeight(double weight)
    {
        secondaryWeight = weight;
        this.RaisePropertyChanged(nameof(HasSecondary));
        UpdateRadarChart(FilteredSessions());
    }

    private static Axis MakeDateAxis(string format, long unitTicks, SolidColorPaint labels, SolidColorPaint grid) =>
        new()
        {
            Labeler = v => { var t = (long)v; return t >= DateTime.MinValue.Ticks && t <= DateTime.MaxValue.Ticks ? new DateTime(t).ToString(format) : string.Empty; },
            UnitWidth = unitTicks,
            MinStep = unitTicks,
            TextSize = 11,
            LabelsPaint = labels,
            SeparatorsPaint = grid,
        };

    private static Axis MakeVolumeAxis(string name, SolidColorPaint labels, SolidColorPaint grid, SolidColorPaint caption) =>
        new()
        {
            Name = name,
            NamePaint = caption,
            NameTextSize = 12,
            TextSize = 11,
            LabelsPaint = labels,
            SeparatorsPaint = grid,
            MinLimit = 0,
        };

    private void UpdateExerciseChart(List<WorkoutSession> filtered)
    {
        if (SelectedExercise == null)
        {
            ExerciseSeries = [];
            HasExerciseData = false;
            return;
        }

        exerciseYAxis.Name = metric switch
        {
            ChartMetric.MaxWeight => $"Max Weight ({WeightHelper.Unit(imperial)})",
            ChartMetric.MaxReps => "Max Reps",
            _ => $"Total Volume ({WeightHelper.Unit(imperial)})"
        };

        var dates = filtered.Select(s => s.Date).ToHashSet();
        var points = exerciseHistory.GetValueOrDefault(SelectedExercise.Id, [])
            .Where(x => dates.Contains(x.Date) && x.Sets.Count > 0)
            .Select(x =>
            {
                var y = metric switch
                {
                    ChartMetric.MaxWeight => WeightHelper.ToDisplay(x.Sets.Max(set => set.Kg), imperial),
                    ChartMetric.MaxReps => x.Sets.Max(set => (double)set.Reps),
                    _ => WeightHelper.ToDisplay(x.Sets.Sum(set => set.Volume), imperial)
                };
                return new DateTimePoint(x.Date.ToDateTime(TimeOnly.MinValue), y);
            })
            .ToArray();

        HasExerciseData = points.Length > 0;

        ExerciseSeries = HasExerciseData
            ? (ISeries[])
            [
                new LineSeries<DateTimePoint>
                {
                    Name = SelectedExercise.Name,
                    Values = points,
                    Fill = new SolidColorPaint(AccentColor.WithAlpha(25)),
                    Stroke = new SolidColorPaint(AccentColor, 2),
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(AccentColor, 2),
                    GeometryFill = new SolidColorPaint(CardBg),
                    LineSmoothness = 0,
                }
            ]
            : [];
    }

    private void UpdateWeeklyChart(List<WorkoutSession> filtered)
    {
        var points = filtered
            .GroupBy(s =>
            {
                var dt = s.Date.ToDateTime(TimeOnly.MinValue);
                var daysFromMonday = ((int)dt.DayOfWeek + 6) % 7; // weekly chart groups by Monday
                return dt.Ticks >= daysFromMonday * TimeSpan.TicksPerDay ? dt.AddDays(-daysFromMonday).Date : dt.Date;
            })
            .Select(g => new DateTimePoint(g.Key, WeightHelper.ToDisplay(g.Sum(s => s.TotalVolume), imperial))).OrderBy(p => p.DateTime).ToArray();

        HasWeeklyData = points.Length > 0;
        WeeklySeries = HasWeeklyData
            ? (ISeries[])
            [
                new ColumnSeries<DateTimePoint>
                {
                    Name = $"Weekly Volume ({WeightHelper.Unit(imperial)})",
                    Values = points,
                    Fill = new SolidColorPaint(AccentColor.WithAlpha(190)),
                    Stroke = null,
                    MaxBarWidth = 32,
                }
            ]
            : [];
    }

    private void UpdateWorkoutChart(List<WorkoutSession> filtered)
    {
        var points = filtered.Select(s => s.Date).Distinct().Select(date =>
            new DateTimePoint(date.ToDateTime(TimeOnly.MinValue), WeightHelper.ToDisplay(sessionVolumes[date], imperial))).ToArray();

        HasWorkoutData = points.Length > 0;
        if (HasWorkoutData)
        {
            var series = new ColumnSeries<DateTimePoint>
            {
                Name = $"Total Volume ({WeightHelper.Unit(imperial)})",
                Values = points,
                Fill = new SolidColorPaint(AccentColor.WithAlpha(190)),
                Stroke = null,
                MaxBarWidth = 22,
            };
            // pointer down only records which bar is under the cursor, the view commits it on release,
            // a pan/drag that happens to start on a bar does not open the workout dialog
            series.ChartPointPointerDown += (_, point) =>
            {
                if (point.Context.DataSource is not DateTimePoint dtp)
                    return;
                var date = DateOnly.FromDateTime(dtp.DateTime);
                pendingClickedSession = filtered.FirstOrDefault(s => s.Date == date);
            };
            WorkoutSeries = [series];
        }
        else
        {
            WorkoutSeries = [];
        }
    }

    private void UpdateRadarChart(List<WorkoutSession> filtered)
    {
        var primarySpoke = CountSpokeSets(filtered, 0);

        HasRadarData = primarySpoke.Sum() > 0;
        if (!HasRadarData)
        {
            RadarSeries = [];
            return;
        }

        var seriesList = new List<ISeries>();
        // if secondary, append to the graph! because secondary always adds into the muscle volume
        if (secondaryWeight > 0)
        {
            var combinedSpoke = CountSpokeSets(filtered, secondaryWeight);
            seriesList.Add(new PolarLineSeries<double>
            {
                Name = "With Secondary",
                Values = combinedSpoke,
                IsClosed = true,
                Fill = new SolidColorPaint(SecondaryColor.WithAlpha(40)),
                Stroke = new SolidColorPaint(SecondaryColor, 2),
                GeometrySize = 0,
                LineSmoothness = 0,
            });
        }

        seriesList.Add(new PolarLineSeries<double>
        {
            Name = "Primary",
            Values = primarySpoke,
            IsClosed = true,
            Fill = new SolidColorPaint(AccentColor.WithAlpha(60)),
            Stroke = new SolidColorPaint(AccentColor, 2),
            GeometrySize = 0,
            LineSmoothness = 0,
        });

        RadarSeries = [.. seriesList];
    }

    private static double[] CountSpokeSets(List<WorkoutSession> filtered, double secondaryWeight)
    {
        var totals = new double[RadarSpokes.Length];
        foreach (var entry in filtered.SelectMany(s => s.Entries))
        {
            var exercise = ExerciseCatalog.Get(entry.ExerciseId);
            if (exercise == null)
                continue;
            for (var i = 0; i < RadarSpokes.Length; i++)
            {
                var muscles = RadarSpokes[i].Muscles;
                if (muscles.Any(m => exercise.PrimaryMuscles.Contains(m)))
                    totals[i] += entry.Sets.Count;
                else if (muscles.Any(m => exercise.SecondaryMuscles.Contains(m)))
                    totals[i] += entry.Sets.Count * secondaryWeight;
            }
        }
        return totals;
    }
}
