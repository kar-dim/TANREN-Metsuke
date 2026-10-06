using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.ViewModels;
using TANREN_Metsuke.Views;
using Xunit;

namespace TANREN_Metsuke.Tests;

// Load real view resources on the headless platform without starting desktop storage or sync.
public sealed class UiTestApplication : Application
{
    public override void Initialize()
    {
        // Load real resources without starting the production application's storage or sync lifecycle.
        var source = new App();
        source.Initialize();
        var resources = source.Resources;
        source.Resources = new ResourceDictionary();
        Resources = resources;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        LiveCharts.Configure(config => config.AddSkiaSharp().AddDefaultMappers());
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<UiTestApplication>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// Check actual view templates and layout so model tests cannot hide binding or virtualization problems.
public sealed class ViewTests
{
    [Fact]
    public async Task MuscleHistoryVirtualizesDaysRatherThanCreatingEveryRow()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(UiTestApplication));
        await session.Dispatch(() =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var workouts = Enumerable.Range(0, 1000).Select(i => new WorkoutSession
            {
                Date = today.AddDays(-i),
                Entries = [new() { ExerciseId = "bench_press", Sets = [new() { Reps = 10, Kg = 30 }] }]
            }).OrderBy(s => s.Date).ToList();
            var view = new SummaryView { DataContext = new SummaryViewModel(workouts, 0.5) };
            var window = new Window { Width = 1200, Height = 800, Content = view };
            window.Show();
            // Open the history panel without pointer input and count only rows created for the visible area.
            var list = view.FindControl<ListBox>("DetailDaysList")!;
            list.ItemsSource = ((SummaryViewModel)view.DataContext!).CreateDetailViewModel(MuscleGroup.Chest).Days;
            view.FindControl<Border>("DetailPanel")!.Width = 500;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1000, list.ItemCount);
            Assert.NotEmpty(list.GetVisualDescendants().OfType<ListBoxItem>());
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(), 1, 30);
            window.Close();
        }, CancellationToken.None);
    }

    [Fact]
    public async Task BodyweightRecordTemplateBindsWithoutNullLoadRecords()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(UiTestApplication));
        await session.Dispatch(() =>
        {
            var workouts = new List<WorkoutSession> { new() { Date = DateOnly.FromDateTime(DateTime.Today),
                Entries = [new() { ExerciseId = "push_up", Sets = [new() { Reps = 20, Kg = 0 }] }] } };
            var model = new RecordsViewModel(workouts);
            var view = new RecordsView { DataContext = model };
            var window = new Window { Width = 1200, Height = 800, Content = view };
            window.Show();
            view.FindControl<Border>("DetailPanel")!.Width = 550;
            view.FindControl<ListBox>("ExerciseRecordsList")!.ItemsSource = model.GetRecords(MuscleGroup.Chest);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var visibleText = view.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text).ToArray();
            Assert.Contains("Most reps", visibleText);
            Assert.Contains("20 reps (no added weight)", visibleText);
            Assert.DoesNotContain("Best weight", visibleText);
            window.Close();
        }, CancellationToken.None);
    }
}
