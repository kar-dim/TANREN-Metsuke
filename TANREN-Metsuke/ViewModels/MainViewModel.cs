using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// order must match the TabItem order in MainView.axaml
public enum AppTab { Home, Graphs, Records, Settings, Sync, Info }

// The main view model for the application, responsible for managing the state of the main window and coordinating between different sub view models
public class MainViewModel : ViewModelBase, IDisposable
{
    public SettingsViewModel Settings { get; }
    public SyncViewModel Sync { get; }

    private List<WorkoutSession> sessions = [];
    private readonly SemaphoreSlim reloadGate = new(1);
    private bool disposed;
    public bool HasWorkouts => sessions.Count > 0;
    private string dataWarning = "";
    public string DataWarning
    {
        get => dataWarning;
        private set
        {
            this.RaiseAndSetIfChanged(ref dataWarning, value);
            this.RaisePropertyChanged(nameof(HasDataWarning));
        }
    }
    public bool HasDataWarning => DataWarning.Length > 0;

    private int selectedTabIndex;
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
    }

    private RecordsViewModel records = null!;
    public RecordsViewModel Records
    {
        get => records;
        private set => this.RaiseAndSetIfChanged(ref records, value);
    }

    private SummaryViewModel summary = null!;
    public SummaryViewModel Summary
    {
        get => summary;
        private set => this.RaiseAndSetIfChanged(ref summary, value);
    }

    private GraphsViewModel graphs = null!;
    public GraphsViewModel Graphs
    {
        get => graphs;
        private set => this.RaiseAndSetIfChanged(ref graphs, value);
    }

    // initialize the main model: load settings, create sync view model and load workout sessions
    public MainViewModel(List<WorkoutSession> sessions, AppSettings settings)
    {
        Settings = new SettingsViewModel(settings,
            onSecondaryChanged: weight => { Summary?.Recompute(weight); Graphs?.UpdateSecondaryWeight(weight); },
            onUnitChanged: () =>
            {
                Summary?.UpdateImperial(settings.UseImperial);
                Graphs?.UpdateImperial(settings.UseImperial);
                Records?.UpdateImperial(settings.UseImperial);
            });

        Sync = new SyncViewModel(
            getFolder: () => SettingsService.WorkoutsFolder,
            onSyncCompleted: ReloadAsync);

        Load(sessions);
    }

    public void Reload(List<WorkoutSession> sessions) => Load(sessions);

    private void Load(List<WorkoutSession> sessions)
    {
        this.sessions = sessions;
        var imperial = Settings.UseImperial;
        Summary = new SummaryViewModel(sessions, Settings.CurrentSecondaryWeight, imperial);
        if (graphs == null)
            Graphs = new GraphsViewModel(sessions, imperial, Settings.CurrentSecondaryWeight);
        else
            Graphs.UpdateSessions(sessions);
        Records = new RecordsViewModel(sessions, imperial);
        this.RaisePropertyChanged(nameof(HasWorkouts));
    }

    public async Task ReloadAsync()
    {
        await reloadGate.WaitAsync();
        try
        {
            if (disposed)
                return;
            var snapshot = await Task.Run(() =>
            {
                lock (WorkoutStorage.Gate)
                {
                    List<string> warnings = [];
                    var loaded = new JsonWorkoutRepository(SettingsService.WorkoutsFolder, warnings.Add).LoadAll();
                    var custom = ExerciseCatalog.ReadCustomExercises(SettingsService.WorkoutsFolder, warnings.Add);
                    return (loaded, custom, warnings);
                }
            });
            if (disposed)
                return;
            ExerciseCatalog.SetCustomExercises(snapshot.custom);
            Load(snapshot.loaded);
            DataWarning = string.Join(Environment.NewLine, snapshot.warnings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DataWarning = $"Could not reload workouts: {ex.Message}. The displayed data has been retained.";
        }
        finally { reloadGate.Release(); }
    }

    public void Dispose()
    {
        disposed = true;
        Sync.Dispose();
    }

    public static List<WorkoutSession> LoadSessions()
    {
        var folder = SettingsService.WorkoutsFolder;
        if (!Directory.Exists(folder))
            return [];
        return new JsonWorkoutRepository(folder).LoadAll();
    }
}
